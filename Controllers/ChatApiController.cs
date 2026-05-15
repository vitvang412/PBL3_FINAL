using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DaNangSafeMap.Services.Implementations;

namespace DaNangSafeMap.Controllers.Api
{
    /// <summary>
    /// API endpoints cho Chat — chức năng 4 (polling 5s)
    /// </summary>
    [ApiController]
    [Route("api/chat")]
    public class ChatApiController : ControllerBase
    {
        private readonly MpChatService _chat;
        public ChatApiController(MpChatService chat) => _chat = chat;

        // POST /api/chat/open-room
        // Người cung cấp bấm "Chat" → mở/tạo phòng
        [HttpPost("open-room")]
        public async Task<IActionResult> OpenRoom([FromBody] OpenRoomRequest req)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { error = "Cần đăng nhập để chat" });

            int senderId = int.Parse(userIdClaim);
            var room = await _chat.GetOrCreateRoomAsync(req.MissingPersonId, senderId);
            return Ok(new { roomId = room.Id, roomName = room.Name });
        }

        // GET /api/chat/messages/{roomId}
        // Load tin nhắn (polling mỗi 5 giây)
        [HttpGet("messages/{roomId}")]
        public async Task<IActionResult> GetMessages(int roomId)
        {
            var msgs = await _chat.GetMessagesAsync(roomId);
            var result = msgs.Select(m => new {
                m.Id,
                m.SenderId,
                SenderName = m.Sender?.FullName ?? "Ẩn danh",
                m.Message,
                SentAt = m.SentAt.ToString("HH:mm dd/MM"),
                m.IsRead
            });
            return Ok(result);
        }

        // POST /api/chat/send
        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendMessageRequest req)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { error = "Cần đăng nhập để gửi tin" });

            int senderId = int.Parse(userIdClaim);
            var msg = await _chat.SendMessageAsync(req.RoomId, senderId, req.Message);
            return Ok(new { success = true, msgId = msg.Id });
        }

        // POST /api/chat/mark-read/{roomId}
        [HttpPost("mark-read/{roomId}")]
        public async Task<IActionResult> MarkRead(int roomId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            int userId = int.Parse(userIdClaim);
            await _chat.MarkReadAsync(roomId, userId);
            return Ok(new { success = true });
        }

        // GET /api/chat/check-unread?missingPersonIds=1,2,3
        // Polling badge đỏ cho người thân
        [HttpGet("check-unread")]
        public async Task<IActionResult> CheckUnread([FromQuery] string missingPersonIds)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Ok(new { count = 0 });

            int ownerId = int.Parse(userIdClaim);
            var ids = missingPersonIds.Split(',')
                .Select(s => int.TryParse(s.Trim(), out var n) ? n : -1)
                .Where(n => n > 0).ToList();

            int count = await _chat.CountUnreadForOwnerAsync(ownerId, ids);
            return Ok(new { count });
        }

        // GET /api/chat/rooms/{missingPersonId}
        // Người thân xem danh sách phòng chat của bài đăng
        [HttpGet("rooms/{missingPersonId}")]
        public async Task<IActionResult> GetRooms(int missingPersonId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? currentUserId = string.IsNullOrEmpty(userIdClaim) ? null : int.Parse(userIdClaim);

            var rooms = await _chat.GetRoomsByMissingPersonAsync(missingPersonId);
            var result = new List<object>();
            foreach (var r in rooms)
            {
                int unread = currentUserId.HasValue
                    ? await _chat.CountUnreadInRoomAsync(r.Id, currentUserId.Value)
                    : 0;
                result.Add(new { r.Id, r.Name, r.CreatedAt, UnreadCount = unread });
            }
            return Ok(result);
        }

        // GET /api/chat/unread-me/{roomId}
        // Guest kiểm tra tin chưa đọc trong phòng của mình (khi owner reply)
        [HttpGet("unread-me/{roomId}")]
        public async Task<IActionResult> UnreadMe(int roomId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Ok(new { count = 0 });
            int userId = int.Parse(userIdClaim);
            int count = await _chat.CountUnreadInRoomAsync(roomId, userId);
            return Ok(new { count });
        }
    }

    public class OpenRoomRequest  { public int MissingPersonId { get; set; } }
    public class SendMessageRequest { public int RoomId { get; set; } public string Message { get; set; } = ""; }
}