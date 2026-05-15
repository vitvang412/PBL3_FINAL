using DaNangSafeMap.Data;
using DaNangSafeMap.Models.DTOs;
using DaNangSafeMap.Models.Entities;
using DaNangSafeMap.Models.ViewModels.Admin;
using DaNangSafeMap.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DaNangSafeMap.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _db;
        private readonly NotificationService _notif;

        public ReportService(ApplicationDbContext db, NotificationService notif)
        {
            _db = db;
            _notif = notif;
        }

        // ─── TẠO BÁO CÁO (dùng bởi người dùng) ─────────────────────────────────
        public async Task<Report> CreateReportAsync(ReportDto dto, int? reporterId)
        {
            try
            {
                // Đảm bảo bảng reports tồn tại và các cột đúng định dạng (có thể null)
                Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(_db.Database, @"
                    CREATE TABLE IF NOT EXISTS `reports` (
                      `Id` int NOT NULL AUTO_INCREMENT,
                      `ReporterId` int DEFAULT NULL,
                      `TargetId` int NOT NULL,
                      `TargetType` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
                      `Reason` text COLLATE utf8mb4_unicode_ci NOT NULL,
                      `Details` text COLLATE utf8mb4_unicode_ci DEFAULT NULL,
                      `Status` int DEFAULT '1',
                      `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
                      PRIMARY KEY (`Id`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                ");
                try { Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(_db.Database, "ALTER TABLE `reports` DROP FOREIGN KEY `FK_Report_User`;"); } catch { }
                try { Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(_db.Database, "ALTER TABLE `reports` MODIFY COLUMN `ReporterId` int NULL;"); } catch { }
                try { Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(_db.Database, "ALTER TABLE `reports` ADD COLUMN `Details` text COLLATE utf8mb4_unicode_ci DEFAULT NULL;"); } catch { }
            }
            catch { }

            var report = new Report
            {
                ReporterId = reporterId,
                TargetId = dto.TargetId,
                TargetType = dto.TargetType,
                Reason = dto.Reason,
                Details = dto.Details,
                Status = 1, // Pending
                CreatedAt = DateTime.UtcNow
            };

            _db.Reports.Add(report);
            await _db.SaveChangesAsync();

            return report;
        }

        // ─── LẤY DANH SÁCH BÁO CÁO (admin) ─────────────────────────────────────
        public async Task<(List<ReportAdminViewModel> Items, int TotalCount)> GetAllReportsAsync(
            string? search, int? status, int page, int pageSize)
        {
            var query = from r in _db.Reports
                        join mp in _db.MissingPersons on r.TargetId equals mp.Id into mpGroup
                        from mp in mpGroup.DefaultIfEmpty()
                        join postOwner in _db.Users on (mp != null ? mp.UserId : 0) equals postOwner.Id into ownerGroup
                        from postOwner in ownerGroup.DefaultIfEmpty()
                        join reporter in _db.Users on r.ReporterId equals reporter.Id into repGroup
                        from reporter in repGroup.DefaultIfEmpty()
                        where r.TargetType == "MissingPerson"
                        select new ReportAdminViewModel
                        {
                            ReportId          = r.Id,
                            CreatedAt         = r.CreatedAt,
                            ReporterName      = reporter != null ? reporter.FullName : "Ẩn danh",
                            ReporterEmail     = reporter != null ? reporter.Email : null,
                            MissingPersonId   = mp != null ? mp.Id : 0,
                            MissingPersonName = mp != null ? mp.FullName : "(Đã xoá)",
                            PostOwnerName     = postOwner != null ? postOwner.FullName : "(Không rõ)",
                            PostOwnerEmail    = postOwner != null ? postOwner.Email : null,
                            PostOwnerId       = postOwner != null ? postOwner.Id : 0,
                            Reason            = r.Reason,
                            Details           = r.Details,
                            Status            = r.Status
                        };

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x =>
                    x.MissingPersonName.Contains(search) ||
                    x.ReporterName.Contains(search) ||
                    x.Reason.Contains(search));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // ─── XỬ LÝ BÁO CÁO: Xoá bài + Thông báo + Đóng báo cáo ────────────────
        public async Task<bool> ResolveAndDeleteAsync(int reportId)
        {
            var report = await _db.Reports.FindAsync(reportId);
            if (report == null) return false;

            // 1. Tìm bài đăng MissingPerson bị báo cáo
            var mp = await _db.MissingPersons
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == report.TargetId);

            string postTitle = "(bài đăng không xác định)";
            int? ownerId = null;

            if (mp != null && mp.DeletedAt == null)
            {
                postTitle = mp.FullName;
                ownerId = mp.UserId;

                // 2. Soft-delete bài đăng
                mp.DeletedAt = DateTime.Now;
                mp.Status = 4; // Deleted
            }

            // 3. Đánh dấu báo cáo là đã xử lý
            report.Status = 2;

            // 4. Đóng tất cả báo cáo khác của cùng bài đăng này
            var samePostReports = await _db.Reports
                .Where(r => r.TargetId == report.TargetId &&
                            r.TargetType == "MissingPerson" &&
                            r.Status == 1)
                .ToListAsync();
            samePostReports.ForEach(r => r.Status = 2);

            await _db.SaveChangesAsync();

            // 5. Gửi thông báo cho chủ bài đăng (nếu có)
            if (ownerId.HasValue && ownerId.Value > 0)
            {
                await _notif.CreateAsync(
                    userId:  ownerId.Value,
                    type:    "AdminAction",
                    title:   "Bài đăng của bạn đã bị gỡ xuống",
                    message: $"Bài đăng tìm người \"{postTitle}\" của bạn đã bị gỡ xuống do vi phạm quy định cộng đồng sau khi nhận được báo cáo từ người dùng. Nếu bạn cho rằng đây là nhầm lẫn, vui lòng liên hệ quản trị viên.",
                    link:    null
                );
            }

            return true;
        }

        // ─── BỎ QUA BÁO CÁO (giữ bài, chỉ đóng báo cáo) ───────────────────────
        public async Task<bool> DismissReportAsync(int reportId)
        {
            var report = await _db.Reports.FindAsync(reportId);
            if (report == null) return false;
            report.Status = 3;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}