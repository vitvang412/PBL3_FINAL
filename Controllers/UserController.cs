using Microsoft.AspNetCore.Mvc;

namespace DaNangSafeMap.Controllers
{
    public class UserController : Controller
    {
        // GET: /User/Dashboard
        public IActionResult Dashboard() => View();
    }
}
