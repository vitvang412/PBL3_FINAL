using Microsoft.AspNetCore.Mvc;
using DaNangSafeMap.Models.DTOs;
using DaNangSafeMap.Services.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DaNangSafeMap.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] ReportDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Reason))
            {
                return BadRequest(new { error = "Dữ liệu không hợp lệ." });
            }

            int? reporterId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int id))
            {
                reporterId = id;
            }

            var report = await _reportService.CreateReportAsync(dto, reporterId);

            return Ok(new { success = true, reportId = report.Id });
        }
    }
}