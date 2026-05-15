using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DaNangSafeMap.Models.ViewModels.MissingPerson;
using DaNangSafeMap.Services.Interfaces;

namespace DaNangSafeMap.Controllers
{
    public class MissingPersonController : Controller
    {
        private readonly IMissingPersonService _service;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public MissingPersonController(IMissingPersonService service, IWebHostEnvironment env, IConfiguration config)
        {
            _service = service;
            _env = env;
            _config = config;
        }

        // GET /MissingPerson
        public async Task<IActionResult> Index(
            string? keyword,
            string? ageGroup,
            string? gender,
            int? daysAgo,
            string sortBy = "newest")
        {
            // Lấy userId nếu đã đăng nhập
            int? currentUserId = null;
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var uid))
                currentUserId = uid;

            var vm = await _service.SearchAsync(keyword, ageGroup, gender, daysAgo, sortBy, currentUserId);
            return View(vm);
        }

        // GET /MissingPerson/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var person = await _service.GetByIdAsync(id);
            if (person == null) return NotFound();
            // Truyền Goong Maps keys vào view
            ViewData["GoongMaptileKey"] = _config["GoongMaps:MaptileKey"];
            ViewData["GoongRestApiKey"] = _config["GoongMaps:RestApiKey"];
            return View(person);
        }

        // GET /MissingPerson/Create
        public IActionResult Create()
        {
            ViewData["GoongMaptileKey"] = _config["GoongMaps:MaptileKey"];
            ViewData["GoongRestApiKey"] = _config["GoongMaps:RestApiKey"];
            return View(new CreateMissingPersonViewModel());
        }

        // POST /MissingPerson/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMissingPersonViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Lấy userId từ JWT cookie
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                TempData["Error"] = "Bạn cần đăng nhập để đăng bài";
                return RedirectToAction("Login", "Auth");
            }
            int userId = int.Parse(userIdClaim);

            // Upload ảnh
            string imageUrl = "/images/missing/default.png";
            if (model.Photo != null && model.Photo.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "missing");
                Directory.CreateDirectory(uploadsFolder);

                var ext = Path.GetExtension(model.Photo.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await model.Photo.CopyToAsync(stream);

                imageUrl = $"/images/missing/{fileName}";
            }

            await _service.CreateAsync(model, userId, imageUrl);

            TempData["Success"] = "Đăng bài tìm người thành công! Bài đăng đang chờ admin duyệt.";
            return RedirectToAction(nameof(Index));
        }

        // POST /MissingPerson/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);
            await _service.DeleteAsync(id, userId);

            TempData["Success"] = "Đã xóa bài đăng";
            return RedirectToAction(nameof(Index));
        }

        // POST /MissingPerson/MarkResolved/5 — Chức năng 5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkResolved(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);
            var ok = await _service.MarkResolvedAsync(id, userId);

            TempData[ok ? "Success" : "Error"] = ok
                ? "🟢 Đã cập nhật trạng thái: Đã tìm thấy!"
                : "Bạn không có quyền thực hiện thao tác này.";

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}