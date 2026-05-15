using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DaNangSafeMap.Services.Implementations;

namespace DaNangSafeMap.Controllers
{
    /// <summary>
    /// API endpoints cho Manh Mối (Chức năng 3)
    /// POST /Clue/Submit      — Gửi manh mối (khách hoặc đã đăng nhập)
    /// GET  /Clue/GetClues/5  — Lấy danh sách manh mối (chỉ owner)
    /// </summary>
    [Route("[controller]/[action]")]
    public class ClueController : Controller
    {
        private readonly ClueService _clueService;
        private readonly IWebHostEnvironment _env;

        public ClueController(ClueService clueService, IWebHostEnvironment env)
        {
            _clueService = clueService;
            _env = env;
        }

        // POST /Clue/Submit
        [HttpPost]
        public async Task<IActionResult> Submit(
            [FromForm] int MissingPersonId,
            [FromForm] string SeenLocation,
            [FromForm] string SeenAt,
            [FromForm] string Content,
            [FromForm] string? Phone,
            [FromForm] bool IsAnonymous = false,
            [FromForm] IFormFile? Photo = null)
        {
            if (string.IsNullOrWhiteSpace(SeenLocation) || string.IsNullOrWhiteSpace(Content))
                return BadRequest(new { error = "Vui lòng điền đầy đủ thông tin bắt buộc." });

            if (!DateTime.TryParse(SeenAt, out var seenAtDt))
                return BadRequest(new { error = "Thời gian không hợp lệ." });

            if (seenAtDt >= DateTime.Now)
                return BadRequest(new { error = "Thời gian thấy phải ở trong quá khứ." });

            // Lấy userId nếu đã đăng nhập
            int? userId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var uid))
                userId = IsAnonymous ? null : uid;

            // Upload ảnh
            string? imageUrl = null;
            if (Photo != null && Photo.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "clues");
                Directory.CreateDirectory(uploadsFolder);
                var ext = Path.GetExtension(Photo.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await Photo.CopyToAsync(stream);
                imageUrl = $"/images/clues/{fileName}";
            }

            await _clueService.CreateAsync(MissingPersonId, userId, SeenLocation, seenAtDt, Content, Phone, imageUrl);

            return Ok(new { success = true });
        }

        // GET /Clue/GetClues/{missingPersonId}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetClues(int id)
        {
            var clues = await _clueService.GetByMissingPersonAsync(id);

            var result = clues.Select(c => new
            {
                c.Id,
                SenderName = c.User?.FullName ?? "Người ẩn danh",
                Location   = c.Location ?? "",
                SeenAt     = ClueService.GetField(c.Description, "SEEN_AT"),
                Content    = ClueService.GetField(c.Description, "CONTENT"),
                Phone      = ClueService.GetField(c.Description, "PHONE"),
                c.ImageUrl,
                CreatedAt  = c.CreatedAt
            });

            return Ok(result);
        }
    }
}
