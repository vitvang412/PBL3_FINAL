using MP = DaNangSafeMap.Models.Entities.MissingPerson;

namespace DaNangSafeMap.Models.ViewModels.MissingPerson
{
    /// <summary>
    /// ViewModel trả về từ MissingPersonService.SearchAsync()
    /// Chứa danh sách kết quả + thống kê để hiển thị trên trang Index.
    /// </summary>
    public class MissingPersonIndexViewModel
    {
        // ── Danh sách bài đăng đã lọc ──
        public List<MP> Items { get; set; } = new();

        // ── Tham số tìm kiếm (để bind lại vào form) ──
        public string? Keyword  { get; set; }
        public string? AgeGroup { get; set; }
        public string? Gender   { get; set; }
        public int?    DaysAgo  { get; set; }
        public string  SortBy   { get; set; } = "newest";

        // ── Thống kê nhanh ──
        public int TotalCount     { get; set; }
        public int SearchingCount { get; set; }   // Status == 1 (đang tìm)
        public int FoundCount     { get; set; }   // Status == 2 (đã tìm thấy)
        public int Near72hCount   { get; set; }   // 48h–72h từ lúc đăng (khẩn cấp)
        public int MyPostsCount   { get; set; }   // Bài của user hiện tại
    }
}