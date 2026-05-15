using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaNangSafeMap.Models.Entities
{
    /// <summary>
    /// Lưu lịch sử kiểm duyệt của admin đối với từng SecurityAlert.
    /// </summary>
    [Table("ModerationLogs")]
    public class ModerationLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AlertId { get; set; }

        [Required]
        public int AdminUserId { get; set; }

        /// <summary>Loại hành động: APPROVE | REJECT | REQUEST_INFO | RESOLVE | etc.</summary>
        [Required]
        [MaxLength(50)]
        public string ActionType { get; set; } = string.Empty;

        /// <summary>Trạng thái trước khi admin thao tác.</summary>
        [MaxLength(30)]
        public string? PreviousStatus { get; set; }

        /// <summary>Trạng thái sau khi admin thao tác (lấy từ alert.Status).</summary>
        [MaxLength(30)]
        public string? NewStatus { get; set; }

        /// <summary>Lý do / ghi chú ngắn của admin.</summary>
        [MaxLength(500)]
        public string? Reason { get; set; }

        /// <summary>Chi tiết bổ sung (JSON hoặc text dài).</summary>
        [MaxLength(2000)]
        public string? Detail { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ── Navigation Properties ──
        [ForeignKey("AlertId")]
        public SecurityAlert Alert { get; set; } = null!;

        [ForeignKey("AdminUserId")]
        public User AdminUser { get; set; } = null!;
    }
}
