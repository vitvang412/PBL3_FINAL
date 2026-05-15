using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaNangSafeMap.Models.Entities
{
    [Table("ModerationLogs")]
    public class ModerationLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AlertId { get; set; }

        [Required]
        public int AdminUserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ActionType { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? PreviousStatus { get; set; }

        [MaxLength(30)]
        public string? NewStatus { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        [MaxLength(1000)]
        public string? Detail { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(AlertId))]
        public SecurityAlert Alert { get; set; } = null!;

        [ForeignKey(nameof(AdminUserId))]
        public User AdminUser { get; set; } = null!;
    }
}
