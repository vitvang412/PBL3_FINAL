using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DaNangSafeMap.Services.Interfaces;

namespace DaNangSafeMap.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin")]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        // GET: /Admin/Reports
        [HttpGet("Reports")]
        public async Task<IActionResult> Reports(string? search, int? status, int page = 1)
        {
            int pageSize = 10;
            var (items, totalCount) = await _reportService.GetAllReportsAsync(search, status, page, pageSize);

            ViewBag.TotalCount = totalCount;
            ViewBag.Page       = page;
            ViewBag.PageSize   = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.Search     = search ?? "";
            ViewBag.Status     = status?.ToString() ?? "";

            return View("~/Views/Admin/Reports.cshtml", items);
        }

        // POST: /Admin/ResolveReport
        [HttpPost("ResolveReport")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveReport(int id)
        {
            await _reportService.ResolveAndDeleteAsync(id);
            TempData["Success"] = "Đã xử lý báo cáo: bài đăng đã được gỡ xuống và chủ bài đã được thông báo.";
            return RedirectToAction("Reports");
        }

        // POST: /Admin/DismissReport
        [HttpPost("DismissReport")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissReport(int id)
        {
            await _reportService.DismissReportAsync(id);
            TempData["Info"] = "Đã bỏ qua báo cáo này.";
            return RedirectToAction("Reports");
        }
    }
}