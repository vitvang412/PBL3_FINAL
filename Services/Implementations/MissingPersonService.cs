using DaNangSafeMap.Data;
using DaNangSafeMap.Models.Entities;
using DaNangSafeMap.Models.ViewModels.MissingPerson;
using DaNangSafeMap.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DaNangSafeMap.Services.Implementations
{
    public class MissingPersonService : IMissingPersonService
    {
        private readonly ApplicationDbContext _db;

        public MissingPersonService(ApplicationDbContext db)
        {
            _db = db;
        }

        // Lấy tất cả bài đăng đang active và chưa bị xóa
        public async Task<List<MissingPerson>> GetAllActiveAsync()
        {
            return await _db.MissingPersons
                .Include(m => m.User)
                .Where(m => m.Status != 4 && m.DeletedAt == null)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        // Tìm kiếm & lọc nâng cao
        public async Task<MissingPersonIndexViewModel> SearchAsync(
            string? keyword, string? ageGroup, string? gender, int? daysAgo, string sortBy, int? currentUserId = null)
        {
            // 1. Lấy toàn bộ bài active từ DB
            var query = _db.MissingPersons
                .Include(m => m.User)
                .Where(m => m.Status != 4 && m.DeletedAt == null);

            // 2. Lọc theo từ khóa (tên hoặc địa điểm)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(m =>
                    m.FullName.ToLower().Contains(kw) ||
                    m.LastSeenLocation.ToLower().Contains(kw));
            }

            // 3. Lọc theo khoảng thời gian
            if (daysAgo.HasValue && daysAgo.Value > 0)
            {
                var cutoff = DateTime.Now.AddDays(-daysAgo.Value);
                query = query.Where(m => m.CreatedAt >= cutoff);
            }

            // 4. Tải về để filter in-memory (age/gender nằm trong Description)
            var all = await query.ToListAsync();

            // Helper parse description
            static string GetDesc(string? desc, string key)
            {
                if (string.IsNullOrEmpty(desc)) return "";
                foreach (var p in desc.Split('|'))
                    if (p.StartsWith(key + ":")) return p[(key.Length + 1)..];
                return "";
            }

            // 5. Lọc theo nhóm tuổi
            if (!string.IsNullOrEmpty(ageGroup))
            {
                all = all.Where(m =>
                {
                    var ageStr = GetDesc(m.Description, "TUOI");
                    if (!int.TryParse(ageStr, out int age)) return false;
                    return ageGroup switch
                    {
                        "child"   => age < 18,
                        "adult"   => age >= 18 && age < 60,
                        "elderly" => age >= 60,
                        _         => true
                    };
                }).ToList();
            }

            // 6. Lọc theo giới tính
            if (!string.IsNullOrEmpty(gender))
            {
                all = all.Where(m =>
                    GetDesc(m.Description, "GIOITINH").Equals(gender, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // 7. Sắp xếp
            var now = DateTime.Now;
            all = sortBy switch
            {
                // Ưu tiên các vụ sắp chạm mốc 72h (48h–72h tính từ lúc đăng)
                "near72h" => all
                    .OrderBy(m =>
                    {
                        var elapsed = (now - m.CreatedAt).TotalHours;
                        // Vụ đang trong vùng 48-72h lên đầu
                        if (elapsed >= 48 && elapsed <= 72) return 0;
                        if (elapsed < 48) return 1;
                        return 2;
                    })
                    .ThenBy(m => m.CreatedAt)
                    .ToList(),
                _ => all.OrderByDescending(m => m.CreatedAt).ToList()  // newest
            };

            // 8. Thống kê
            var allForStats = await _db.MissingPersons
                .Where(m => m.Status != 4 && m.DeletedAt == null)
                .ToListAsync();

            return new MissingPersonIndexViewModel
            {
                Items          = all,
                Keyword        = keyword,
                AgeGroup       = ageGroup,
                Gender         = gender,
                DaysAgo        = daysAgo,
                SortBy         = sortBy ?? "newest",
                TotalCount     = allForStats.Count,
                SearchingCount = allForStats.Count(m => m.Status == 1),
                FoundCount     = allForStats.Count(m => m.Status == 2),
                Near72hCount   = allForStats.Count(m =>
                {
                    var h = (now - m.CreatedAt).TotalHours;
                    return m.Status == 1 && h >= 48 && h <= 72;
                }),
                MyPostsCount   = currentUserId.HasValue
                    ? allForStats.Count(m => m.UserId == currentUserId.Value)
                    : 0
            };
        }

        // Lấy chi tiết 1 bài
        public async Task<MissingPerson?> GetByIdAsync(int id)
        {
            return await _db.MissingPersons
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id && m.DeletedAt == null);
        }

        // Tạo bài đăng mới
        public async Task<MissingPerson> CreateAsync(CreateMissingPersonViewModel model, int userId, string imageUrl)
        {
            var fullDesc = $"TUOI:{model.Age}|GIOITINH:{model.Gender}|CHIEUCAO:{model.Height}|DACBIET:{model.Features}|LIENHE:{model.ContactInfo}|NOIDUNG:{model.Description}";

            var person = new MissingPerson
            {
                UserId          = userId,
                FullName        = model.FullName,
                Description     = fullDesc,
                LastSeenLocation = model.LastSeenLocation,
                Latitude        = model.Latitude,
                Longitude       = model.Longitude,
                ImageUrl        = imageUrl,
                Status          = 1,
                CreatedAt       = DateTime.Now
            };

            _db.MissingPersons.Add(person);
            await _db.SaveChangesAsync();
            return person;
        }

        // Xóa bài (soft delete)
        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var person = await _db.MissingPersons.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
            if (person == null) return false;

            person.DeletedAt = DateTime.Now;
            person.Status    = 4;
            await _db.SaveChangesAsync();
            return true;
        }

        // Đánh dấu đã tìm thấy — chỉ owner mới làm được
        public async Task<bool> MarkResolvedAsync(int id, int userId)
        {
            var person = await _db.MissingPersons.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
            if (person == null) return false;

            person.Status = 2;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
