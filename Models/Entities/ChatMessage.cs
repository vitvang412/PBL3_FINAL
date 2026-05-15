using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaNangSafeMap.Models.Entities
{
    [Table("chatmessages")]
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        public int RoomId { get; set; }

        public int SenderId { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        // Navigations
        [ForeignKey("RoomId")]
        public ChatRoom? ChatRoom { get; set; }

        [ForeignKey("SenderId")]
        public User? Sender { get; set; }
    }
}