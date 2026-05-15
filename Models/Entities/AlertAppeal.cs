using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaNangSafeMap.Models.Entities
{
    [Table("AlertAppeals")]
    public class AlertAppeal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AlertId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "PENDING";

        public int? ReviewedByAdminId { get; set; }

        [MaxLength(500)]
        public string? ReviewNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ReviewedAt { get; set; }

        [ForeignKey(nameof(AlertId))]
        public SecurityAlert Alert { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(ReviewedByAdminId))]
        public User? ReviewedByAdmin { get; set; }
    }
}
