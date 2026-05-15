using DaNangSafeMap.Models.Entities;
using DaNangSafeMap.Models.ViewModels.Alert;
using Microsoft.AspNetCore.Http;

namespace DaNangSafeMap.Services.Interfaces
{
    /// <summary>
    /// Interface cho AlertService — chứa business logic bản đồ + báo cáo sự cố.
    /// </summary>
    public interface IAlertService
    {
        // ── NV2: Lấy danh sách loại sự cố ──
        Task<List<AlertTypeDto>> GetAlertTypesAsync();

        // ── NV4: Tạo báo cáo mới ──
        Task<SecurityAlert> CreateAlertAsync(CreateAlertViewModel model, int userId);
        Task<AlertMedia> UploadMediaAsync(int alertId, int userId, IFormFile file);
        Task<(string UploadId, int ChunkSize, long MaxFileSize)> CreateMediaUploadSessionAsync(
            int alertId, int userId, string fileName, long fileSize, string? contentType);
        Task SaveMediaChunkAsync(int alertId, int userId, string uploadId, int chunkIndex, Stream chunkStream);
        Task<AlertMedia> CompleteMediaUploadAsync(int alertId, int userId, string uploadId, int totalChunks);

        // ── NV5: Lấy dữ liệu bản đồ ──
        Task<List<AlertMapDto>> GetAlertsForMapAsync(
            decimal southLat, decimal northLat,
            decimal westLng, decimal eastLng,
            DateTime fromTime, DateTime toTime,
            bool includeHidden = false);
        Task<AlertMapDto?> GetAlertDetailAsync(int alertId);
        Task<List<object>> GetHeatmapDataAsync(DateTime fromTime, DateTime toTime);

        // ── NV6: Xác nhận cộng đồng ──
        Task<(bool Success, string Message)> VerifyAlertAsync(
            int alertId, int userId, VerifyAlertViewModel model);

        // ── NV8: Auto-expire ──
        Task ProcessExpiredAlertsAsync();

        // ── My Reports ──
        Task<List<AlertMapDto>> GetMyAlertsAsync(int userId);
        Task<(bool Success, string Message)> SubmitAlertReportAsync(int alertId, int reporterId, string reason, string? description);

        // ── Admin ──
        Task<List<AlertMapDto>> GetPendingAlertsAsync();
        Task<(List<AlertMapDto> Items, int Total)> GetAllAlertsForAdminAsync(string? status, int page, int pageSize);
        Task<bool> ApproveAlertAsync(int alertId, int adminUserId);
        Task<bool> RejectAlertAsync(int alertId, int adminUserId, string reason);
        Task<List<object>> GetAlertReportsForAdminAsync();
        Task<(bool Success, string Message)> HideAlertFromReportAsync(int reportId, int adminId, string? note);
        Task<(bool Success, string Message)> DeleteAlertFromReportAsync(int reportId, int adminId, string? note);
        Task<(bool Success, string Message)> BanUserFromReportAsync(int reportId, int adminId, string? note);
        Task<(bool Success, string Message)> IgnoreAlertReportAsync(int reportId, int adminId, string? note);

        // ── Moderation mở rộng ──
        /// <summary>Không đủ cơ sở — ẩn tin, không phạt user</summary>
        Task<bool> MarkInsufficientAsync(int alertId, int adminId, string reason);

        bool GetNeedsMoreInfoAutoHideEnabled();
        Task SetNeedsMoreInfoAutoHideEnabledAsync(bool enabled);

        /// <summary>Yêu cầu bổ sung bằng chứng</summary>
        Task<bool> RequestMoreInfoAsync(int alertId, int adminId, string reason);

        /// <summary>Đánh dấu sự cố đã xử lý — chủ tin hoặc Admin</summary>
        Task<bool> ResolveAlertAsync(int alertId, int userId);

        /// <summary>Người dùng khiếu nại báo cáo bị bác bỏ trong 24h</summary>
        Task<(bool Success, string Message)> SubmitAppealAsync(int alertId, int userId, string reason);

        /// <summary>Admin chấp nhận khiếu nại và mở lại báo cáo để duyệt lại</summary>
        Task<(bool Success, string Message)> ApproveAppealAsync(int alertId, int adminId, string? reviewNote);

        /// <summary>Admin giữ nguyên bác bỏ sau khi xem khiếu nại</summary>
        Task<(bool Success, string Message)> RejectAppealAsync(int alertId, int adminId, string? reviewNote);

        // ── Comments ──
        Task<List<object>> GetCommentsAsync(int alertId);
        Task<object> AddCommentAsync(int alertId, int userId, string content, IFormFile? mediaFile);
    }
}
