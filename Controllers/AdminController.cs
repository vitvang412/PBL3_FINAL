using System.Security.Claims;
using DaNangSafeMap.Data;
using DaNangSafeMap.Models.Entities;
using DaNangSafeMap.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DaNangSafeMap.Controllers
{
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies,Bearer")]
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private const int DashboardPageSize = 8;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("")]
        [HttpGet("Dashboard")]
        public async Task<IActionResult> Dashboard(string? search, string? role, string? status, int page = 1)
        {
            var query = _db.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(u =>
                    u.FullName.Contains(keyword) ||
                    u.Email.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = status switch
                {
                    "active" => query.Where(u => u.IsActive && !u.IsBanned),
                    "inactive" => query.Where(u => !u.IsActive),
                    "banned" => query.Where(u => u.IsBanned),
                    "locked" => query.Where(u => u.LockedUntil != null && u.LockedUntil > DateTime.UtcNow),
                    _ => query
                };
            }

            page = Math.Max(page, 1);
            var filteredCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)DashboardPageSize));
            if (page > totalPages)
            {
                page = totalPages;
            }

            var currentAdminId = GetCurrentAdminId();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * DashboardPageSize)
                .Take(DashboardPageSize)
                .Select(u => new AdminDashboardUserItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    AuthProvider = u.AuthProvider,
                    IsActive = u.IsActive,
                    IsBanned = u.IsBanned,
                    LockedUntil = u.LockedUntil,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    IsCurrentAdmin = currentAdminId == u.Id,
                    StatusLabel = u.IsBanned
                        ? "Đã chặn"
                        : (!u.IsActive
                            ? "Tạm ngưng"
                            : (u.LockedUntil != null && u.LockedUntil > DateTime.UtcNow
                                ? "Đang khóa"
                                : "Hoạt động")),
                    StatusClass = u.IsBanned
                        ? "danger"
                        : (!u.IsActive
                            ? "muted"
                            : (u.LockedUntil != null && u.LockedUntil > DateTime.UtcNow
                                ? "warn"
                                : "active"))
                })
                .ToListAsync();

            var vm = new AdminDashboardViewModel
            {
                Users = users,
                Search = search?.Trim() ?? string.Empty,
                RoleFilter = role?.Trim() ?? string.Empty,
                StatusFilter = status?.Trim() ?? string.Empty,
                Page = page,
                TotalPages = totalPages,
                FilteredCount = filteredCount,
                TotalUsers = await _db.Users.CountAsync(),
                ActiveUsers = await _db.Users.CountAsync(u => u.IsActive && !u.IsBanned),
                BannedUsers = await _db.Users.CountAsync(u => u.IsBanned),
                AdminUsers = await _db.Users.CountAsync(u => u.Role == "Admin")
            };

            return View(vm);
        }

        [HttpPost("UpdateUserRole")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(int id, string roleValue, string? search, string? role, string? status, int page = 1)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                TempData["AdminError"] = "Không tìm thấy tài khoản cần cập nhật.";
                return RedirectToDashboard(search, role, status, page);
            }

            if (GetCurrentAdminId() == user.Id)
            {
                TempData["AdminError"] = "Không thể tự đổi vai trò của chính bạn.";
                return RedirectToDashboard(search, role, status, page);
            }

            var allowedRoles = new[] { "User", "Moderator", "Admin" };
            if (!allowedRoles.Contains(roleValue))
            {
                TempData["AdminError"] = "Vai trò không hợp lệ.";
                return RedirectToDashboard(search, role, status, page);
            }

            user.Role = roleValue;
            await _db.SaveChangesAsync();

            TempData["AdminSuccess"] = $"Đã cập nhật vai trò cho {user.FullName}.";
            return RedirectToDashboard(search, role, status, page);
        }

        [HttpPost("ToggleUserActive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserActive(int id, string? search, string? role, string? status, int page = 1)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                TempData["AdminError"] = "Không tìm thấy tài khoản cần cập nhật.";
                return RedirectToDashboard(search, role, status, page);
            }

            if (GetCurrentAdminId() == user.Id)
            {
                TempData["AdminError"] = "Không thể tự khóa hoặc tự tạm ngưng chính bạn.";
                return RedirectToDashboard(search, role, status, page);
            }

            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();

            TempData["AdminSuccess"] = user.IsActive
                ? $"Đã kích hoạt lại tài khoản {user.FullName}."
                : $"Đã tạm ngưng tài khoản {user.FullName}.";

            return RedirectToDashboard(search, role, status, page);
        }

        [HttpPost("ToggleUserBan")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserBan(int id, string? search, string? role, string? status, int page = 1)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                TempData["AdminError"] = "Không tìm thấy tài khoản cần cập nhật.";
                return RedirectToDashboard(search, role, status, page);
            }

            if (GetCurrentAdminId() == user.Id)
            {
                TempData["AdminError"] = "Không thể tự chặn tài khoản của chính bạn.";
                return RedirectToDashboard(search, role, status, page);
            }

            user.IsBanned = !user.IsBanned;
            if (user.IsBanned)
            {
                user.IsActive = false;
                user.LockedUntil ??= DateTime.UtcNow.AddYears(10);
            }
            else if (user.LockedUntil != null && user.LockedUntil > DateTime.UtcNow.AddYears(5))
            {
                user.LockedUntil = null;
            }

            await _db.SaveChangesAsync();

            TempData["AdminSuccess"] = user.IsBanned
                ? $"Đã chặn tài khoản {user.FullName}."
                : $"Đã gỡ chặn tài khoản {user.FullName}.";

            return RedirectToDashboard(search, role, status, page);
        }

        [HttpGet("AlertModeration")]
        public IActionResult AlertModeration()
        {
            return View();
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            Response.Cookies.Delete("jwtToken");
            return RedirectToAction("Login", "Auth");
        }

        private int? GetCurrentAdminId()
        {
            var rawId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(rawId, out var adminId) ? adminId : null;
        }

        private IActionResult RedirectToDashboard(string? search, string? role, string? status, int page)
        {
            return RedirectToAction(nameof(Dashboard), new
            {
                search,
                role,
                status,
                page
            });
        }
    }
}
