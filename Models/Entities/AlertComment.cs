using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaNangSafeMap.Models.Entities
{
    /// <summary>
    /// Bình luận của người dùng trên từng báo cáo sự cố.
    /// </summary>
    [Table("AlertComments")]
    public class AlertComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AlertId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? MediaUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ── Navigation Properties ──
        [ForeignKey("AlertId")]
        public SecurityAlert Alert { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
