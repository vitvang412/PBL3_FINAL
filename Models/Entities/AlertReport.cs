using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaNangSafeMap.Models.Entities
{
    [Table("AlertReports")]
    public class AlertReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AlertId { get; set; }

        [Required]
        public int ReporterId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "PENDING";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(AlertId))]
        public SecurityAlert Alert { get; set; } = null!;

        [ForeignKey(nameof(ReporterId))]
        public User Reporter { get; set; } = null!;
    }
}
