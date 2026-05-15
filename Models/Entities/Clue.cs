using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaNangSafeMap.Models.Entities
{
    [Table("clues")]
    public class Clue
    {
        [Key]
        public int Id { get; set; }

        public int MissingPersonId { get; set; }

        public int? UserId { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;  // Chứa packed data: LOC:|SEEN:|PHONE:|CONTENT:

        [MaxLength(500)]
        public string? Location { get; set; }

        [MaxLength(255)]
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigations
        [ForeignKey("MissingPersonId")]
        public MissingPerson? MissingPerson { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}