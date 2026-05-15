using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaNangSafeMap.Models.Entities
{
    /// <summary>
    /// Represents a user report for a post (e.g., MissingPerson) for spam, fraud, etc.
    /// </summary>
    [Table("reports")]
    public class Report
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Id of the user who reported. Nullable for anonymous reports.
        /// </summary>
        public int? ReporterId { get; set; }

        /// <summary>
        /// The target entity id being reported (e.g., MissingPerson Id).
        /// </summary>
        public int TargetId { get; set; }

        /// <summary>
        /// Type of the target entity. For now only "MissingPerson" is used.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string TargetType { get; set; } = "MissingPerson";

        /// <summary>
        /// Reason selected by the reporter.
        /// </summary>
        [Required]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Additional details provided when Reason is "Other".
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Status of the report: 1 = Pending, 2 = Resolved/Handled.
        /// </summary>
        public int Status { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}