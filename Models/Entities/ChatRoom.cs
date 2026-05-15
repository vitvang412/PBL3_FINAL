using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaNangSafeMap.Models.Entities
{
    [Table("chatrooms")]
    public class ChatRoom
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(255)]
        public string? Name { get; set; }  // Format: "MP_{missingPersonId}_U_{senderId}"

        public bool IsGroup { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}