using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DaNangSafeMap.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin")]
    public class DashboardController : Controller
    {
        // GET: /Admin  hoặc  /Admin/Dashboard
        [HttpGet("")]
        [HttpGet("Dashboard")]
        public IActionResult Dashboard()
        {
            return View("~/Views/Admin/Dashboard.cshtml");
        }

        // POST: /Admin/Logout
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            Response.Cookies.Delete("jwtToken");
            return RedirectToAction("Login", "Auth");
        }
    }
}