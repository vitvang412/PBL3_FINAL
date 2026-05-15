using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DaNangSafeMap.Models.ViewModels.Alert;
using DaNangSafeMap.Services.Interfaces;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace DaNangSafeMap.Controllers
{
    /// <summary>
    /// API Controller cho bản đồ + báo cáo sự cố.
    /// Trả JSON cho frontend JavaScript gọi.
    /// </summary>
    [Route("api/alerts")]
    [ApiController]
    public class AlertController : ControllerBase
    {
        private readonly IAlertService _alertService;

        public AlertController(IAlertService alertService)
        {
            _alertService = alertService;
        }

        // ═══════════════════════════════════════════════
        // GET /api/alerts/types — Danh sách loại sự cố
        // ═══════════════════════════════════════════════
        [HttpGet("types")]
        public async Task<IActionResult> GetAlertTypes()
        {
            var types = await _alertService.GetAlertTypesAsync();
            return Ok(types);
        }

        // ═══════════════════════════════════════════════
        // GET /api/alerts/map — Markers cho bản đồ
        // ═══════════════════════════════════════════════
        [HttpGet("map")]
        public async Task<IActionResult> GetMapAlerts(
            [FromQuery] decimal southLat,
            [FromQuery] decimal northLat,
            [FromQuery] decimal westLng,
            [FromQuery] decimal eastLng,
            [FromQuery] DateTime? fromTime,
            [FromQuery] DateTime? toTime,
            [FromQuery] bool includeHidden = false)
        {
            if (includeHidden)
            {
                var cookieAuth = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var jwtAuth = cookieAuth.Succeeded
                    ? cookieAuth
                    : await HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

                if (!jwtAuth.Succeeded)
                    return Unauthorized(new { message = "Vui lòng đăng nhập để xem tin đã ẩn" });
            }

            var from = fromTime ?? DateTime.Now.AddHours(-24);
            var to = toTime ?? DateTime.Now;
            var alerts = await _alertService.GetAlertsForMapAsync(
                southLat, northLat, westLng, eastLng, from, to, includeHidden);
            return Ok(alerts);
        }

        // ═══════════════════════════════════════════════
        // GET /api/alerts/heatmap — Dữ liệu heatmap
        // ═══════════════════════════════════════════════
        [HttpGet("heatmap")]
        public async Task<IActionResult> GetHeatmapData(
            [FromQuery] DateTime? fromTime,
            [FromQuery] DateTime? toTime)
        {
            var from = fromTime ?? DateTime.Now.AddDays(-30);
            var to = toTime ?? DateTime.Now;
            var data = await _alertService.GetHeatmapDataAsync(from, to);
            return Ok(data);
        }

        // ═══════════════════════════════════════════════
        // GET /api/alerts/{id} — Chi tiết 1 alert
        // ═══════════════════════════════════════════════
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAlertDetail(int id)
        {
            var alert = await _alertService.GetAlertDetailAsync(id);
            if (alert == null)
                return NotFound(new { message = "Không tìm thấy báo cáo" });
            return Ok(alert);
        }

        // ═══════════════════════════════════════════════
        // POST /api/alerts — Tạo báo cáo mới
        // ═══════════════════════════════════════════════
        [HttpPost]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> CreateAlert([FromForm] CreateAlertViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var alert = await _alertService.CreateAlertAsync(model, userId);

                return Ok(new
                {
                    success = true,
                    message = BuildAlertStatusMessage(alert.Status),
                    alertId = alert.Id,
                    status = alert.Status,
                    routingDecision = alert.RoutingDecision
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════
        // POST /api/alerts/{id}/media — Upload ảnh
        // ═══════════════════════════════════════════════
        [HttpPost("{id}/media")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> UploadMedia(int id, IFormFile file)
        {
            try
            {
                var userId = GetUserId();
                var media = await _alertService.UploadMediaAsync(id, userId, file);
                var alert = await _alertService.GetAlertDetailAsync(id);

                return Ok(new
                {
                    success = true,
                    filePath = media.FilePath,
                    message = BuildMediaStatusMessage(alert?.Status),
                    status = alert?.Status,
                    routingDecision = alert?.RoutingDecision
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════
        // POST /api/alerts/{id}/media/session — Tạo phiên upload chunk
        // ═══════════════════════════════════════════════
        [HttpPost("{id}/media/session")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> CreateMediaUploadSession(int id, [FromBody] CreateMediaUploadSessionRequest body)
        {
            if (body == null || string.IsNullOrWhiteSpace(body.FileName) || body.FileSize <= 0)
                return BadRequest(new { success = false, message = "Thông tin file không hợp lệ" });

            try
            {
                var userId = GetUserId();
                var session = await _alertService.CreateMediaUploadSessionAsync(
                    id, userId, body.FileName, body.FileSize, body.ContentType);

                return Ok(new
                {
                    success = true,
                    uploadId = session.UploadId,
                    chunkSize = session.ChunkSize,
                    maxFileSize = session.MaxFileSize
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════
        // POST /api/alerts/{id}/media/chunk — Upload một chunk
        // ═══════════════════════════════════════════════
        [HttpPost("{id}/media/chunk")]
        [RequestSizeLimit(8 * 1024 * 1024)]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> UploadMediaChunk(
            int id,
            [FromQuery] string uploadId,
            [FromQuery] int chunkIndex)
        {
            if (string.IsNullOrWhiteSpace(uploadId))
                return BadRequest(new { success = false, message = "Thiếu phiên upload" });

            try
            {
                var userId = GetUserId();
                await _alertService.SaveMediaChunkAsync(id, userId, uploadId, chunkIndex, Request.Body);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════
        // POST /api/alerts/{id}/media/complete — Hoàn tất upload
        // ═══════════════════════════════════════════════
        [HttpPost("{id}/media/complete")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> CompleteMediaUpload(int id, [FromBody] CompleteMediaUploadRequest body)
        {
            if (body == null || string.IsNullOrWhiteSpace(body.UploadId) || body.TotalChunks <= 0)
                return BadRequest(new { success = false, message = "Thông tin hoàn tất upload không hợp lệ" });

            try
            {
                var userId = GetUserId();
                var media = await _alertService.CompleteMediaUploadAsync(id, userId, body.UploadId, body.TotalChunks);
                var alert = await _alertService.GetAlertDetailAsync(id);

                return Ok(new
                {
                    success = true,
                    filePath = media.FilePath,
                    message = BuildMediaStatusMessage(alert?.Status),
                    status = alert?.Status,
                    routingDecision = alert?.RoutingDecision
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════
        // POST /api/alerts/{id}/verify — Xác nhận cộng đồng
        // ═══════════════════════════════════════════════
        [HttpPost("{id}/verify")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> VerifyAlert(int id, [FromBody] VerifyAlertViewModel model)
        {
            try
            {
                var userId = GetUserId();
                var (success, message) = await _alertService.VerifyAlertAsync(id, userId, model);
                if (!success)
                    return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private static string BuildAlertStatusMessage(string? status)
        {
            return status switch
            {
                "VISIBLE_UNVERIFIED" => "Báo cáo đã được ghi nhận và đang hiển thị ở trạng thái Chưa xác thực.",
                "PENDING_REVIEW" => "Báo cáo đã được ghi nhận và chuyển vào hàng chờ kiểm duyệt.",
                "REJECTED" => "Báo cáo đã được ghi nhận nhưng đang bị ẩn do không vượt qua bộ lọc tự động.",
                _ => "Báo cáo đã được ghi nhận."
            };
        }

        private static string BuildMediaStatusMessage(string? status)
        {
            return status switch
            {
                "VISIBLE_UNVERIFIED" => "Đã bổ sung bằng chứng. Báo cáo đang hiển thị ở trạng thái Chưa xác thực.",
                "PENDING_REVIEW" => "Đã bổ sung bằng chứng. Báo cáo vẫn đang chờ kiểm duyệt.",
                "REJECTED" => "Đã nhận file bằng chứng nhưng báo cáo vẫn bị ẩn do bộ lọc tự động.",
                _ => "Đã tải bằng chứng thành công."
            };
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) throw new UnauthorizedAccessException("Vui lòng đăng nhập");
            return int.Parse(claim.Value);
        }

        // ═══════════════════════════════════════════════
        // GET /api/alerts/my — Báo cáo của tôi
        // ═══════════════════════════════════════════════
        [HttpGet("my")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetMyAlerts()
        {
            var userId = GetUserId();
            var alerts = await _alertService.GetMyAlertsAsync(userId);
            return Ok(alerts);
        }

        [HttpPost("{id}/report")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> SubmitAlertReport(int id, [FromBody] AlertReportRequest body)
        {
            if (string.IsNullOrWhiteSpace(body.Reason))
                return BadRequest(new { success = false, message = "Vui lòng chọn lý do báo cáo" });

            if (body.Description?.Length > 300)
                return BadRequest(new { success = false, message = "Mô tả thêm tối đa 300 ký tự" });

            var userId = GetUserId();
            var (success, message) = await _alertService.SubmitAlertReportAsync(id, userId, body.Reason, body.Description);
            return success
                ? Ok(new { success = true, message })
                : BadRequest(new { success = false, message });
        }

        // ═══════════════════════════════════════════════
        // ADMIN ENDPOINTS
        // ═══════════════════════════════════════════════

        // GET /api/alerts/admin/pending — Danh sách chờ duyệt
        [HttpGet("admin/pending")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetPendingAlerts()
        {
            var alerts = await _alertService.GetPendingAlertsAsync();
            return Ok(alerts);
        }

        // GET /api/alerts/admin/all?status=&page=&pageSize=
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetAllAlertsAdmin(
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var (items, total) = await _alertService.GetAllAlertsForAdminAsync(status, page, pageSize);
            return Ok(new { items, total, page, pageSize });
        }

        [HttpGet("admin/reports")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetAlertReportsAdmin()
        {
            var items = await _alertService.GetAlertReportsForAdminAsync();
            return Ok(items);
        }

        [HttpPost("admin/reports/{reportId}/hide")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> HideAlertFromReport(int reportId, [FromBody] ModerationRequest body)
        {
            var adminId = GetUserId();
            var (success, message) = await _alertService.HideAlertFromReportAsync(reportId, adminId, body.Reason);
            return success ? Ok(new { success = true, message }) : BadRequest(new { success = false, message });
        }

        [HttpPost("admin/reports/{reportId}/delete")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DeleteAlertFromReport(int reportId, [FromBody] ModerationRequest body)
        {
            var adminId = GetUserId();
            var (success, message) = await _alertService.DeleteAlertFromReportAsync(reportId, adminId, body.Reason);
            return success ? Ok(new { success = true, message }) : BadRequest(new { success = false, message });
        }

        [HttpPost("admin/reports/{reportId}/ban")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> BanUserFromReport(int reportId, [FromBody] ModerationRequest body)
        {
            var adminId = GetUserId();
            var (success, message) = await _alertService.BanUserFromReportAsync(reportId, adminId, body.Reason);
            return success ? Ok(new { success = true, message }) : BadRequest(new { success = false, message });
        }

        [HttpPost("admin/reports/{reportId}/ignore")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> IgnoreAlertReport(int reportId, [FromBody] ModerationRequest body)
        {
            var adminId = GetUserId();
            var (success, message) = await _alertService.IgnoreAlertReportAsync(reportId, adminId, body.Reason);
            return success ? Ok(new { success = true, message }) : BadRequest(new { success = false, message });
        }

        // POST /api/alerts/admin/{id}/approve
        [HttpPost("admin/{id}/approve")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ApproveAlert(int id)
        {
            var adminId = GetUserId();
            var ok = await _alertService.ApproveAlertAsync(id, adminId);
            return ok
                ? Ok(new { success = true, message = "Đã duyệt và hiển thị trên bản đồ" })
                : NotFound(new { success = false, message = "Không tìm thấy báo cáo" });
        }

        // POST /api/alerts/admin/{id}/reject
        [HttpPost("admin/{id}/reject")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> RejectAlert(int id, [FromBody] ModerationRequest body)
        {
            var adminId = GetUserId();
            var ok = await _alertService.RejectAlertAsync(id, adminId, body.Reason ?? "Không đủ thông tin");
            return ok
                ? Ok(new { success = true, message = "Đã bác bỏ báo cáo vi phạm" })
                : NotFound(new { success = false, message = "Không tìm thấy báo cáo" });
        }

        // POST /api/alerts/admin/{id}/insufficient — Không đủ cơ sở
        [HttpPost("admin/{id}/insufficient")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> MarkInsufficient(int id, [FromBody] ModerationRequest body)
        {
            if (string.IsNullOrWhiteSpace(body.Reason))
                return BadRequest(new { success = false, message = "Lý do không được để trống" });
            var adminId = GetUserId();
            var ok = await _alertService.MarkInsufficientAsync(id, adminId, body.Reason);
            return ok
                ? Ok(new { success = true, message = "Đã đánh dấu không đủ cơ sở" })
                : NotFound(new { success = false, message = "Không tìm thấy báo cáo" });
        }

        [HttpGet("admin/needs-more-info-auto-hide")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult GetNeedsMoreInfoAutoHide()
        {
            return Ok(new { enabled = _alertService.GetNeedsMoreInfoAutoHideEnabled() });
        }

        [HttpPost("admin/needs-more-info-auto-hide")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> SetNeedsMoreInfoAutoHide([FromBody] NeedsMoreInfoAutoHideRequest body)
        {
            await _alertService.SetNeedsMoreInfoAutoHideEnabledAsync(body.Enabled);
            return Ok(new
            {
                success = true,
                enabled = _alertService.GetNeedsMoreInfoAutoHideEnabled(),
                message = body.Enabled
                    ? "Đã bật tự ẩn tin cần bổ sung sau 24 giờ"
                    : "Đã tắt tự ẩn tin cần bổ sung sau 24 giờ"
            });
        }

        // POST /api/alerts/admin/{id}/request-info — Yêu cầu bổ sung bằng chứng
        [HttpPost("admin/{id}/request-info")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> RequestMoreInfo(int id, [FromBody] ModerationRequest body)
        {
            if (string.IsNullOrWhiteSpace(body.Reason))
                return BadRequest(new { success = false, message = "Lý do không được để trống" });
            var adminId = GetUserId();
            var ok = await _alertService.RequestMoreInfoAsync(id, adminId, body.Reason);
            return ok
                ? Ok(new
                {
                    success = true,
                    message = _alertService.GetNeedsMoreInfoAutoHideEnabled()
                        ? "Đã yêu cầu bổ sung — người dùng có 24h"
                        : "Đã yêu cầu bổ sung — báo cáo không tự ẩn sau 24h"
                })
                : NotFound(new { success = false, message = "Không tìm thấy báo cáo" });
        }

        // POST /api/alerts/{id}/appeal — Người dùng khiếu nại báo cáo bị bác bỏ
        [HttpPost("{id}/appeal")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> SubmitAppeal(int id, [FromBody] AppealRequest body)
        {
            var userId = GetUserId();
            var (success, message) = await _alertService.SubmitAppealAsync(id, userId, body.Reason ?? string.Empty);
            return success
                ? Ok(new { success = true, message })
                : BadRequest(new { success = false, message });
        }

        // POST /api/alerts/admin/{id}/appeal/approve — Chấp nhận khiếu nại
        [HttpPost("admin/{id}/appeal/approve")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ApproveAppeal(int id, [FromBody] ModerationRequest body)
        {
            var adminId = GetUserId();
            var (success, message) = await _alertService.ApproveAppealAsync(id, adminId, body.Reason);
            return success
                ? Ok(new { success = true, message })
                : BadRequest(new { success = false, message });
        }

        // POST /api/alerts/admin/{id}/appeal/reject — Giữ nguyên bác bỏ sau khi xem khiếu nại
        [HttpPost("admin/{id}/appeal/reject")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> RejectAppeal(int id, [FromBody] ModerationRequest body)
        {
            var adminId = GetUserId();
            var (success, message) = await _alertService.RejectAppealAsync(id, adminId, body.Reason);
            return success
                ? Ok(new { success = true, message })
                : BadRequest(new { success = false, message });
        }

        // POST /api/alerts/{id}/resolve — Đánh dấu đã xử lý (chủ tin hoặc Admin)
        [HttpPost("{id}/resolve")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ResolveAlert(int id)
        {
            try
            {
                var userId = GetUserId();
                var ok = await _alertService.ResolveAlertAsync(id, userId);
                return ok
                    ? Ok(new { success = true, message = "Đã đánh dấu sự cố đã xử lý" })
                    : BadRequest(new { success = false, message = "Không có quyền hoặc không tìm thấy báo cáo" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════
        // GET /api/alerts/{id}/comments — Lấy danh sách bình luận
        // ═══════════════════════════════════════════════
        [HttpGet("{id}/comments")]
        public async Task<IActionResult> GetComments(int id)
        {
            var comments = await _alertService.GetCommentsAsync(id);
            return Ok(comments);
        }

        // ═══════════════════════════════════════════════
        // POST /api/alerts/{id}/comments — Thêm bình luận mới
        // ═══════════════════════════════════════════════
        [HttpPost("{id}/comments")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> AddComment(int id, [FromForm] CommentRequest body)
        {
            if (body == null || (string.IsNullOrWhiteSpace(body.Content) && body.MediaFile == null))
                return BadRequest(new { success = false, message = "Bình luận phải có nội dung hoặc file đính kèm" });

            if (body.Content?.Length > 500)
                return BadRequest(new { success = false, message = "Bình luận tối đa 500 ký tự" });

            try
            {
                var userId = GetUserId();
                var comment = await _alertService.AddCommentAsync(id, userId, body.Content?.Trim() ?? "", body.MediaFile);
                return Ok(new { success = true, comment });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    // Helper records
    public record RejectAlertRequest(string? Reason);
    public record AlertReportRequest(string? Reason, string? Description);
    public record ModerationRequest(string? Reason);
    public record NeedsMoreInfoAutoHideRequest(bool Enabled);
    public record AppealRequest(string? Reason);
    public record CreateMediaUploadSessionRequest(string FileName, long FileSize, string? ContentType);
    public record CompleteMediaUploadRequest(string UploadId, int TotalChunks);
    public record CommentRequest(string? Content, IFormFile? MediaFile);
}
