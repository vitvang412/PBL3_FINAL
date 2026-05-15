using System;

namespace DaNangSafeMap.Models.ViewModels.Admin
{
    /// <summary>
    /// ViewModel dùng để hiển thị báo cáo trong trang admin,
    /// kết hợp thông tin từ Report + MissingPerson + User.
    /// </summary>
    public class ReportAdminViewModel
    {
        public int ReportId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Người báo cáo
        public string ReporterName { get; set; } = "Ẩn danh";
        public string? ReporterEmail { get; set; }

        // Bài đăng bị báo cáo
        public int MissingPersonId { get; set; }
        public string MissingPersonName { get; set; } = string.Empty;
        public string PostOwnerName { get; set; } = string.Empty;
        public string? PostOwnerEmail { get; set; }
        public int PostOwnerId { get; set; }

        // Nội dung báo cáo
        public string Reason { get; set; } = string.Empty;
        public string? Details { get; set; }

        // Trạng thái: 1=Chờ xử lý, 2=Đã xử lý, 3=Bỏ qua
        public int Status { get; set; }
        public string StatusLabel => Status switch
        {
            1 => "Chờ xử lý",
            2 => "Đã xử lý",
            3 => "Bỏ qua",
            _ => "Không xác định"
        };
        public string StatusBadgeClass => Status switch
        {
            1 => "badge-pending",
            2 => "badge-resolved",
            3 => "badge-dismissed",
            _ => "badge-pending"
        };

        public string ReasonLabel => Reason switch
        {
            "spam"       => "Spam / Quảng cáo",
            "fake"       => "Thông tin giả mạo",
            "duplicate"  => "Bài đăng trùng lặp",
            "offensive"  => "Nội dung phản cảm",
            "other"      => "Lý do khác",
            _            => Reason
        };
    }
}