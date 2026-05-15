using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DaNangSafeMap.Services.Implementations;
using Microsoft.AspNetCore.Authorization;

namespace DaNangSafeMap.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly NotificationService _notifService;

        public NotificationController(NotificationService notifService)
        {
            _notifService = notifService;
        }

        private int? GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var id)) return id;
            return null;
        }

        [HttpGet("unread")]
        public async Task<IActionResult> GetUnread()
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized();

            var list = await _notifService.GetUnreadAsync(userId.Value);
            return Ok(list);
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent()
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized();

            var list = await _notifService.GetRecentAsync(userId.Value);
            return Ok(list);
        }

        [HttpPost("read/{id}")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized();

            await _notifService.MarkReadAsync(id, userId.Value);
            return Ok(new { success = true });
        }

        [HttpPost("readAll")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized();

            await _notifService.MarkAllReadAsync(userId.Value);
            return Ok(new { success = true });
        }
    }
}
