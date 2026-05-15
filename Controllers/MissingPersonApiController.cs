using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DaNangSafeMap.Data;

using Microsoft.AspNetCore.Authorization;

namespace DaNangSafeMap.Controllers.Api
{
    [ApiController]
    [Route("api/missing-person")]
    [Authorize]
    public class MissingPersonApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public MissingPersonApiController(ApplicationDbContext db) => _db = db;

        /// <summary>
        /// GET /api/missing-person/my-count
        /// Trả về số bài đăng của user hiện tại (không bị ảnh hưởng bởi search/filter)
        /// </summary>
        [HttpGet("my-count")]
        public async Task<IActionResult> MyCount()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Ok(new { count = 0 });

            int userId = int.Parse(userIdClaim);
            int count = await _db.MissingPersons
                .CountAsync(m => m.UserId == userId && m.Status != 4 && m.DeletedAt == null);

            return Ok(new { count });
        }
    }
}