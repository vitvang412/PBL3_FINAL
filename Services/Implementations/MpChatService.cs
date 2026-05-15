using DaNangSafeMap.Data;
using DaNangSafeMap.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DaNangSafeMap.Services.Implementations
{
    public class MpChatService
    {
        private readonly ApplicationDbContext _db;
        private readonly NotificationService _notif;

        public MpChatService(ApplicationDbContext db, NotificationService notif)
        {
            _db = db;
            _notif = notif;
        }

        // Tìm hoặc tạo phòng chat giữa người cung cấp và bài đăng
        public async Task<ChatRoom> GetOrCreateRoomAsync(int missingPersonId, int senderUserId)
        {
            var roomName = $"MP_{missingPersonId}_U_{senderUserId}";
            var room = await _db.ChatRooms.FirstOrDefaultAsync(r => r.Name == roomName);
            if (room == null)
            {
                room = new ChatRoom { Name = roomName, IsGroup = false };
                _db.ChatRooms.Add(room);
                await _db.SaveChangesAsync();
            }
            return room;
        }

        // Lấy lịch sử tin nhắn của 1 phòng
        public async Task<List<ChatMessage>> GetMessagesAsync(int roomId)
        {
            return await _db.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.RoomId == roomId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        // Gửi tin nhắn
        public async Task<ChatMessage> SendMessageAsync(int roomId, int senderId, string text)
        {
            var msg = new ChatMessage
            {
                RoomId = roomId,
                SenderId = senderId,
                Message = text,
                SentAt = DateTime.Now,
                IsRead = false
            };
            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();

            // Gửi thông báo liên quan đến chat
            var room = await _db.ChatRooms.FirstOrDefaultAsync(r => r.Id == roomId);
            if (room?.Name != null && room.Name.StartsWith("MP_"))
            {
                // Tên phòng: MP_{mpId}_U_{senderUserId}
                var parts = room.Name.Split('_');
                if (parts.Length >= 4
                    && int.TryParse(parts[1], out int mpId)
                    && int.TryParse(parts[3], out int chatUserId))
                {
                    var post = await _db.MissingPersons.FirstOrDefaultAsync(m => m.Id == mpId);
                    if (post != null)
                    {
                        if (post.UserId != senderId)
                        {
                            // Người dùng nhắn → notify chủ bài
                            bool isAnon = text.StartsWith("[ANON]");
                            string senderLabel = isAnon
                                ? "Người ẩn danh"
                                : (await _db.Users.FindAsync(senderId))?.FullName ?? "Ai đó";
                            await _notif.CreateAsync(
                                userId: post.UserId,
                                type: "chat",
                                title: "Tin nhắn mới",
                                message: $"{senderLabel} vừa nhắn tin về bài đăng \u201c{post.FullName}\u201d",
                                link: $"/MissingPerson/Details/{mpId}#chat"
                            );
                        }
                        else if (senderId == post.UserId && chatUserId != senderId)
                        {
                            // Chủ bài reply → notify người đã nhắn tin
                            var ownerName = (await _db.Users.FindAsync(senderId))?.FullName ?? "Chủ bài đăng";
                            await _notif.CreateAsync(
                                userId: chatUserId,
                                type: "chat",
                                title: "Tin nhắn mới",
                                message: $"{ownerName} vừa phản hồi về bài đăng \u201c{post.FullName}\u201d",
                                link: $"/MissingPerson/Details/{mpId}#chat"
                            );
                        }
                    }
                }
            }

            return msg;
        }

        // Đánh dấu đã đọc tất cả tin nhắn trong phòng (trừ tin của chính mình)
        public async Task MarkReadAsync(int roomId, int currentUserId)
        {
            var unread = await _db.ChatMessages
                .Where(m => m.RoomId == roomId && m.SenderId != currentUserId && !m.IsRead)
                .ToListAsync();
            unread.ForEach(m => m.IsRead = true);
            await _db.SaveChangesAsync();
        }

        // Đếm tổng tin chưa đọc của 1 user (owner của bài đăng)
        // rooms có chứa "_U_" sẽ có ownerId từ bài đăng, nhưng ta đếm theo pattern tên phòng
        public async Task<int> CountUnreadForOwnerAsync(int ownerUserId, List<int> missingPersonIds)
        {
            if (!missingPersonIds.Any()) return 0;

            // Lấy tên phòng pattern: "MP_{id}_U_"
            var patterns = missingPersonIds.Select(id => $"MP_{id}_U_").ToList();

            // Tải toàn bộ rooms rồi lọc trên client (tránh lỗi EF primitive collection)
            var allRooms = await _db.ChatRooms
                .Where(r => r.Name != null)
                .ToListAsync();

            var matchedRooms = allRooms
                .Where(r => patterns.Any(p => r.Name!.StartsWith(p)))
                .ToList();

            if (!matchedRooms.Any()) return 0;

            var roomIds = matchedRooms.Select(r => r.Id).ToList();
            return await _db.ChatMessages
                .CountAsync(m => roomIds.Contains(m.RoomId)
                              && m.SenderId != ownerUserId
                              && !m.IsRead);
        }

        // Lấy tất cả phòng chat liên quan đến 1 bài đăng
        public async Task<List<ChatRoom>> GetRoomsByMissingPersonAsync(int missingPersonId)
        {
            var prefix = $"MP_{missingPersonId}_U_";
            return await _db.ChatRooms
                .Where(r => r.Name != null && r.Name.StartsWith(prefix))
                .ToListAsync();
        }

        // Đếm tin chưa đọc trong 1 phòng cụ thể cho 1 user
        public async Task<int> CountUnreadInRoomAsync(int roomId, int userId)
        {
            return await _db.ChatMessages
                .CountAsync(m => m.RoomId == roomId
                              && m.SenderId != userId
                              && !m.IsRead);
        }

        // Lấy phòng cụ thể theo tên
        public async Task<ChatRoom?> GetRoomByNameAsync(string name) =>
            await _db.ChatRooms.FirstOrDefaultAsync(r => r.Name == name);
    }
}