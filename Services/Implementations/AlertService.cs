using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using DaNangSafeMap.Data;
using DaNangSafeMap.Models.Entities;
using DaNangSafeMap.Models.ViewModels.Alert;
using DaNangSafeMap.Repositories;
using DaNangSafeMap.Services.Interfaces;

namespace DaNangSafeMap.Services.Implementations
{
    /// <summary>
    /// Business logic cho bản đồ + báo cáo sự cố.
    /// Tính TrustScore, Opacity, xử lý xác nhận cộng đồng, auto-expire.
    /// </summary>
    public class AlertService : IAlertService
    {
        private const int MediaChunkSize = 5 * 1024 * 1024;
        private const long MaxMediaFileSize = 500L * 1024 * 1024;
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] AllowedVideoExtensions = { ".mp4", ".webm" };
        private static readonly string[] OffensiveKeywords = { "địt", "dit me", "đm", "dm", "đéo", "deo", "lồn", "lon", "cặc", "cac", "bú cặc", "bu cac", "fuck", "shit" };
        private static readonly string[] SpamKeywords = { "telegram", "casino", "cá cược", "ca cuoc", "kiếm tiền", "kiem tien", "click link", "truy cập", "truy cap" };
        private static readonly Regex RepeatedCharacterRegex = new(@"(.)\1{7,}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex UrlRegex = new(@"https?://|www\.", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly string[] AllowedAlertReportReasons =
        {
            "THONG_TIN_SAI_SU_THAT",
            "NOI_DUNG_XUC_PHAM",
            "SPAM",
            "NGUOI_DUNG_GIA_MAO",
            "KHAC"
        };

        private readonly IAlertRepository _alertRepo;
        private readonly IUserRepository _userRepo;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly AlertLifecycleRuntimeSettings _lifecycleRuntimeSettings;

        public AlertService(
            IAlertRepository alertRepo,
            IUserRepository userRepo,
            ApplicationDbContext context,
            IWebHostEnvironment env,
            AlertLifecycleRuntimeSettings lifecycleRuntimeSettings)
        {
            _alertRepo = alertRepo;
            _userRepo = userRepo;
            _context = context;
            _env = env;
            _lifecycleRuntimeSettings = lifecycleRuntimeSettings;
        }

        // ═══════════════════════════════════════════════
        // NV2: LẤY DANH SÁCH LOẠI SỰ CỐ
        // ═══════════════════════════════════════════════

        public async Task<List<AlertTypeDto>> GetAlertTypesAsync()
        {
            var types = await _alertRepo.GetAlertTypesAsync();
            return types.Select(t => new AlertTypeDto
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                IconEmoji = t.IconEmoji,
                IconUrl = t.IconUrl,
                CategoryId = t.CategoryId,
                CategoryName = t.Category.Name,
                CategoryColor = t.Category.ColorHex
            }).ToList();
        }

        // ═══════════════════════════════════════════════
        // NV4: TẠO BÁO CÁO MỚI
        // ═══════════════════════════════════════════════

        public async Task<SecurityAlert> CreateAlertAsync(CreateAlertViewModel model, int userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) throw new Exception("Không tìm thấy người dùng");
            EnsureUserCanSubmit(user);

            if (!model.UserConfirmed)
                throw new Exception("Vui lòng xác nhận trách nhiệm trước khi gửi báo cáo");

            var description = model.Description.Trim();
            if (description.Length < 20)
                throw new Exception("Mô tả phải có ít nhất 20 ký tự");

            var title = string.IsNullOrWhiteSpace(model.Title)
                ? "Báo cáo sự cố"
                : model.Title.Trim();

            var alertType = await _alertRepo.GetAlertTypeByIdAsync(model.AlertTypeId);
            if (alertType == null)
                throw new Exception("Loại sự cố không hợp lệ");

            var evaluation = await EvaluateInitialAlertAsync(user, alertType, title, description);

            // ── Tạo entity ──
            var createdAt = DateTime.Now;
            var alert = new SecurityAlert
            {
                UserId = userId,
                AlertTypeId = model.AlertTypeId,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                AddressText = model.AddressText,
                Title = title,
                Description = description,
                IncidentTime = model.IncidentTime ?? createdAt,
                UserConfirmed = model.UserConfirmed,
                Status = "PENDING_REVIEW",
                TrustScore = evaluation.TrustScore,
                RoutingDecision = evaluation.RoutingDecision,
                FilterReason = evaluation.FilterReason,
                TrustScoreBreakdown = evaluation.TrustScoreBreakdown,
                Opacity = 30,
                HasMedia = false,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

            ApplyRoutingState(alert);
            ApplyLifecycleState(alert, createdAt, resetReviewDue: true);

            var hour = DateTime.Now.Hour;
            bool sensitiveTime = (hour >= 21 || hour <= 4);
            if (alert.Status == "VISIBLE_UNVERIFIED" && sensitiveTime)
            {
                alert.Opacity = Math.Max(alert.Opacity, 50);
            }

            // ── Lưu DB ──
            await _alertRepo.CreateAlertAsync(alert);

            // ── Kiểm tra auto-clustering ──
            var nearbyCount = await _alertRepo.CountNearbyAlertsAsync(
                model.Latitude, model.Longitude, model.AlertTypeId, 30);
            if (nearbyCount >= 3)
            {
                alert.FilterReason = AppendNote(alert.FilterReason, "Có nhiều báo cáo tương tự gần đó trong 30 phút");
                await _alertRepo.UpdateAlertAsync(alert);
            }

            return alert;
        }

        // ═══════════════════════════════════════════════
        // NV4: UPLOAD MEDIA
        // ═══════════════════════════════════════════════

        public async Task<AlertMedia> UploadMediaAsync(int alertId, int userId, IFormFile file)
        {
            var alert = await _alertRepo.GetByIdAsync(alertId);
            if (alert == null) throw new Exception("Không tìm thấy báo cáo");

            var extension = ValidateMediaFile(file.FileName, file.Length);

            // Tạo thư mục (ngoài wwwroot → tránh dotnet watch hot-reload)
            var uploadDir = Path.Combine(_env.ContentRootPath, "App_Data", "uploads", "alerts", alertId.ToString());
            Directory.CreateDirectory(uploadDir);

            // Tên file unique
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadDir, fileName);

            // Lưu file
            await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, useAsync: true))
            {
                await file.CopyToAsync(stream);
            }

            // Tạo entity
            var mediaType = extension is ".mp4" or ".webm" ? "VIDEO" : "IMAGE";
            var media = new AlertMedia
            {
                AlertId = alertId,
                UserId = userId,
                MediaType = mediaType,
                FilePath = $"/uploads/alerts/{alertId}/{fileName}",
                FileName = file.FileName,
                FileSize = file.Length,
                SourceType = alert.UserId == userId ? "ORIGINAL" : "VERIFICATION",
                CreatedAt = DateTime.Now
            };

            await _alertRepo.AddMediaAsync(media);

            if (!alert.HasMedia)
            {
                alert.HasMedia = true;
                alert.TrustScore += 20;
                alert.TrustScoreBreakdown = AppendTrustBreakdown(alert.TrustScoreBreakdown, "+20: có ảnh hoặc video bằng chứng");
                alert.FilterReason = AppendNote(alert.FilterReason, "Đã bổ sung ảnh hoặc video bằng chứng");

                if (!string.Equals(alert.RoutingDecision, "RED", StringComparison.OrdinalIgnoreCase))
                {
                    alert.RoutingDecision = "GREEN";
                }

                alert.UpdatedAt = DateTime.Now;
                ApplyRoutingState(alert, promotedByMedia: true);
                ApplyLifecycleState(alert, alert.UpdatedAt, resetReviewDue: alert.Status == "PENDING_REVIEW");
                await _alertRepo.UpdateAlertAsync(alert);
            }

            return media;
        }

        public async Task<(string UploadId, int ChunkSize, long MaxFileSize)> CreateMediaUploadSessionAsync(
            int alertId, int userId, string fileName, long fileSize, string? contentType)
        {
            var alert = await _alertRepo.GetByIdAsync(alertId);
            if (alert == null) throw new Exception("Không tìm thấy báo cáo");
            if (alert.UserId != userId) throw new Exception("Bạn chỉ có thể tải bằng chứng cho báo cáo của mình");

            var safeFileName = Path.GetFileName(fileName);
            var extension = ValidateMediaFile(safeFileName, fileSize);
            var uploadId = Guid.NewGuid().ToString("N");
            var sessionDir = GetSessionDirectory(alertId, uploadId);
            Directory.CreateDirectory(sessionDir);

            var metadata = new UploadSessionMetadata
            {
                AlertId = alertId,
                UserId = userId,
                FileName = safeFileName,
                Extension = extension,
                FileSize = fileSize,
                ContentType = contentType,
                CreatedAt = DateTime.Now
            };

            var metadataJson = JsonSerializer.Serialize(metadata);
            await File.WriteAllTextAsync(Path.Combine(sessionDir, "metadata.json"), metadataJson);

            return (uploadId, MediaChunkSize, MaxMediaFileSize);
        }

        public async Task SaveMediaChunkAsync(int alertId, int userId, string uploadId, int chunkIndex, Stream chunkStream)
        {
            if (chunkIndex < 0) throw new Exception("Thứ tự chunk không hợp lệ");

            await LoadUploadSessionMetadataAsync(alertId, userId, uploadId);
            var sessionDir = GetSessionDirectory(alertId, uploadId);
            Directory.CreateDirectory(sessionDir);

            var chunkPath = Path.Combine(sessionDir, $"{chunkIndex:D6}.part");
            await using var output = new FileStream(chunkPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, useAsync: true);
            await chunkStream.CopyToAsync(output);
        }

        public async Task<AlertMedia> CompleteMediaUploadAsync(int alertId, int userId, string uploadId, int totalChunks)
        {
            if (totalChunks <= 0) throw new Exception("Số lượng chunk không hợp lệ");

            var alert = await _alertRepo.GetByIdAsync(alertId);
            if (alert == null) throw new Exception("Không tìm thấy báo cáo");
            if (alert.UserId != userId) throw new Exception("Bạn chỉ có thể hoàn tất bằng chứng cho báo cáo của mình");

            var metadata = await LoadUploadSessionMetadataAsync(alertId, userId, uploadId);
            var expectedChunks = (int)Math.Ceiling(metadata.FileSize / (double)MediaChunkSize);
            if (totalChunks != expectedChunks)
                throw new Exception("Số lượng chunk không khớp với phiên upload");

            var sessionDir = GetSessionDirectory(alertId, uploadId);
            var uploadDir = Path.Combine(_env.ContentRootPath, "App_Data", "uploads", "alerts", alertId.ToString());
            Directory.CreateDirectory(uploadDir);

            var finalName = $"{Guid.NewGuid()}{metadata.Extension}";
            var finalPath = Path.Combine(uploadDir, finalName);

            await using (var output = new FileStream(finalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 1024, useAsync: true))
            {
                for (var i = 0; i < totalChunks; i++)
                {
                    var chunkPath = Path.Combine(sessionDir, $"{i:D6}.part");
                    if (!File.Exists(chunkPath))
                        throw new Exception($"Thiếu chunk {i + 1}/{totalChunks}");

                    await using var input = new FileStream(chunkPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, useAsync: true);
                    await input.CopyToAsync(output);
                }
            }

            var finalInfo = new FileInfo(finalPath);
            if (finalInfo.Length != metadata.FileSize)
            {
                File.Delete(finalPath);
                throw new Exception("Kích thước file hoàn tất không khớp với dữ liệu upload");
            }

            var mediaType = metadata.Extension is ".mp4" or ".webm" ? "VIDEO" : "IMAGE";
            var media = new AlertMedia
            {
                AlertId = alertId,
                UserId = userId,
                MediaType = mediaType,
                FilePath = $"/uploads/alerts/{alertId}/{finalName}",
                FileName = metadata.FileName,
                FileSize = metadata.FileSize,
                SourceType = "ORIGINAL",
                CreatedAt = DateTime.Now
            };

            await _alertRepo.AddMediaAsync(media);

            if (!alert.HasMedia)
            {
                alert.HasMedia = true;
                alert.TrustScore += 20;
                alert.TrustScoreBreakdown = AppendTrustBreakdown(alert.TrustScoreBreakdown, "+20: có ảnh hoặc video bằng chứng");
                alert.FilterReason = AppendNote(alert.FilterReason, "Đã bổ sung ảnh hoặc video bằng chứng");

                if (!string.Equals(alert.RoutingDecision, "RED", StringComparison.OrdinalIgnoreCase))
                {
                    alert.RoutingDecision = "GREEN";
                }

                alert.UpdatedAt = DateTime.Now;
                ApplyRoutingState(alert, promotedByMedia: true);
                ApplyLifecycleState(alert, alert.UpdatedAt, resetReviewDue: alert.Status == "PENDING_REVIEW");
                await _alertRepo.UpdateAlertAsync(alert);
            }

            try
            {
                Directory.Delete(sessionDir, recursive: true);
            }
            catch
            {
            }

            return media;
        }

        // ═══════════════════════════════════════════════
        // NV5: LẤY DỮ LIỆU BẢN ĐỒ
        // ═══════════════════════════════════════════════

        public async Task<List<AlertMapDto>> GetAlertsForMapAsync(
            decimal southLat, decimal northLat,
            decimal westLng, decimal eastLng,
            DateTime fromTime, DateTime toTime,
            bool includeHidden = false)
        {
            var alerts = await _alertRepo.GetAlertsForMapAsync(
                southLat, northLat, westLng, eastLng, fromTime, toTime, includeHidden);

            return alerts.Select(MapToDto).ToList();
        }

        public async Task<AlertMapDto?> GetAlertDetailAsync(int alertId)
        {
            var alert = await _alertRepo.GetByIdWithDetailsAsync(alertId);
            if (alert == null || alert.Status == "DELETED") return null;
            return MapToDto(alert);
        }

        public async Task<List<object>> GetHeatmapDataAsync(DateTime fromTime, DateTime toTime)
        {
            var alerts = await _alertRepo.GetHeatmapDataAsync(fromTime, toTime);
            return alerts.Select(a => (object)new
            {
                lat = a.Latitude,
                lng = a.Longitude,
                intensity = Math.Max(1, a.TrustScore / 10 + a.ConfirmCount)
            }).ToList();
        }

        // ═══════════════════════════════════════════════
        // NV6: XÁC NHẬN CỘNG ĐỒNG
        // ═══════════════════════════════════════════════

        public async Task<(bool Success, string Message)> VerifyAlertAsync(
            int alertId, int userId, VerifyAlertViewModel model)
        {
            var alert = await _alertRepo.GetByIdAsync(alertId);
            if (alert == null)
                return (false, "Không tìm thấy báo cáo");

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null || !user.IsActive || user.IsBanned)
                return (false, "Tài khoản của bạn hiện không thể thực hiện thao tác này");

            if (alert.UserId == userId)
                return (false, "Không thể xác nhận báo cáo của chính mình");

            if (alert.Status != "VISIBLE_UNVERIFIED" && alert.Status != "VISIBLE_VERIFIED")
                return (false, "Báo cáo này hiện không nhận xác thực cộng đồng");

            var existing = await _alertRepo.GetVerificationAsync(alertId, userId);

            if (existing != null)
            {
                // Đã xác nhận → kiểm tra đổi ý
                if (existing.PreviousType != null)
                    return (false, "Bạn chỉ được đổi ý 1 lần");

                if (existing.VerificationType == model.VerificationType)
                    return (false, "Bạn đã xác nhận loại này rồi");

                // Cho đổi ý
                existing.PreviousType = existing.VerificationType;
                existing.VerificationType = model.VerificationType;
                existing.ChangedAt = DateTime.Now;
                existing.Comment = model.Comment;
                await _alertRepo.UpdateVerificationAsync(existing);
            }
            else
            {
                // Tạo mới
                var verification = new AlertVerification
                {
                    AlertId = alertId,
                    UserId = userId,
                    VerificationType = model.VerificationType,
                    Latitude = model.Latitude,
                    Longitude = model.Longitude,
                    Comment = model.Comment,
                    CreatedAt = DateTime.Now
                };
                await _alertRepo.AddVerificationAsync(verification);
            }

            // ── Cập nhật thống kê alert ──
            await RecalculateAlertStats(alert);

            return (true, model.VerificationType == "CONFIRM"
                ? "Cảm ơn bạn đã xác nhận thông tin!"
                : "Cảm ơn bạn đã phản hồi!");
        }

        public bool GetNeedsMoreInfoAutoHideEnabled()
        {
            return _lifecycleRuntimeSettings.NeedsMoreInfoAutoHideEnabled;
        }

        public async Task SetNeedsMoreInfoAutoHideEnabledAsync(bool enabled)
        {
            if (_lifecycleRuntimeSettings.NeedsMoreInfoAutoHideEnabled == enabled)
                return;

            _lifecycleRuntimeSettings.SetNeedsMoreInfoAutoHideEnabled(enabled);

            if (enabled)
                return;

            var now = DateTime.Now;
            var alerts = await _context.SecurityAlerts
                .Where(a => a.Status == "NEEDS_MORE_INFO")
                .ToListAsync();

            foreach (var alert in alerts)
            {
                alert.MoreInfoDeadline = null;
                alert.UpdatedAt = now;
                ApplyLifecycleState(alert, now);
            }

            if (alerts.Count > 0)
            {
                await _context.SaveChangesAsync();
            }
        }

        // ═══════════════════════════════════════════════
        // NV8: AUTO-EXPIRE
        // ═══════════════════════════════════════════════

        public async Task ProcessExpiredAlertsAsync()
        {
            var now = DateTime.Now;
            var needsMoreInfoAutoHideEnabled = GetNeedsMoreInfoAutoHideEnabled();
            var expiredAlerts = await _alertRepo.GetExpiredAlertsAsync(needsMoreInfoAutoHideEnabled);
            foreach (var alert in expiredAlerts)
            {
                if (alert.Status == "EXPIRED")
                    continue;

                alert.Status = "EXPIRED";
                alert.Opacity = 0;
                alert.ModerationReason = needsMoreInfoAutoHideEnabled && alert.MoreInfoDeadline.HasValue && alert.MoreInfoDeadline <= now
                    ? "Quá hạn bổ sung bằng chứng nên báo cáo đã tự động ẩn."
                    : alert.ModerationReason;
                alert.FilterReason = AppendNote(alert.FilterReason, "Tự động ẩn theo vòng đời hoặc SLA");
                alert.UpdatedAt = now;
                ApplyLifecycleState(alert, now);
                await _alertRepo.UpdateAlertAsync(alert);
            }

            var activeLifecycleAlerts = await _context.SecurityAlerts
                .Where(a => a.Status == "VISIBLE_UNVERIFIED" || a.Status == "VISIBLE_VERIFIED" || a.Status == "PENDING_REVIEW" || a.Status == "NEEDS_MORE_INFO")
                .ToListAsync();

            foreach (var alert in activeLifecycleAlerts)
            {
                ApplyLifecycleState(alert, now);
            }

            if (activeLifecycleAlerts.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            if (expiredAlerts.Count > 0)
            {
                Console.WriteLine($"[AlertLifecycle] Đã ẩn {expiredAlerts.Count} alerts theo vòng đời.");
            }
        }

        // ═══════════════════════════════════════════════
        // PRIVATE HELPERS
        // ═══════════════════════════════════════════════

        /// <summary>Đếm lại ConfirmCount/DenyCount và cập nhật Opacity/Status</summary>
        private async Task RecalculateAlertStats(SecurityAlert alert)
        {
            var fullAlert = await _alertRepo.GetByIdWithDetailsAsync(alert.Id);
            if (fullAlert == null) return;

            var confirmCount = fullAlert.Verifications.Count(v => v.VerificationType == "CONFIRM");
            var denyCount = fullAlert.Verifications.Count(v => v.VerificationType == "DENY");
            var previousStatus = fullAlert.Status;

            fullAlert.ConfirmCount = confirmCount;
            fullAlert.DenyCount = denyCount;

            if (confirmCount >= 3 && confirmCount > denyCount)
            {
                fullAlert.Status = "VISIBLE_VERIFIED";
                fullAlert.Opacity = 100;
                fullAlert.ModerationReason = null;
                fullAlert.MoreInfoDeadline = null;
            }
            else if (denyCount >= 3 && denyCount > confirmCount)
            {
                fullAlert.Status = "PENDING_REVIEW";
                fullAlert.Opacity = 20;
                fullAlert.ModerationReason = "Cộng đồng đã phủ nhận từ 3 lần trở lên. Cần admin xem lại.";
                fullAlert.FilterReason = AppendNote(fullAlert.FilterReason, "Cộng đồng phủ nhận nhiều lần, chuyển lại hàng chờ admin");
            }
            else if (confirmCount > 0 && fullAlert.Status != "VISIBLE_VERIFIED")
            {
                fullAlert.Opacity = Math.Min(30 + (confirmCount * 20), 80);
            }

            fullAlert.UpdatedAt = DateTime.Now;
            ApplyLifecycleState(fullAlert, fullAlert.UpdatedAt, resetReviewDue: fullAlert.Status == "PENDING_REVIEW" && previousStatus != "PENDING_REVIEW");
            await _alertRepo.UpdateAlertAsync(fullAlert);

            if (previousStatus != "PENDING_REVIEW" && fullAlert.Status == "PENDING_REVIEW" && denyCount >= 3)
            {
                await CreateNotificationsForAdminsAsync(
                    fullAlert.Id,
                    "Báo cáo cần xem lại",
                    $"Báo cáo #{fullAlert.Id} đã bị cộng đồng phủ nhận từ 3 lần trở lên và được chuyển lại hàng chờ duyệt.",
                    "COMMUNITY_REVIEW");
            }
        }

        private static void ApplyRoutingState(SecurityAlert alert, bool promotedByMedia = false)
        {
            var routing = string.IsNullOrWhiteSpace(alert.RoutingDecision)
                ? "YELLOW"
                : alert.RoutingDecision.Trim().ToUpperInvariant();

            alert.RoutingDecision = routing;

            switch (routing)
            {
                case "RED":
                    alert.Status = "REJECTED";
                    alert.Opacity = 0;
                    break;

                case "GREEN":
                    if (alert.Status != "VISIBLE_VERIFIED" && alert.Status != "RESOLVED")
                    {
                        alert.Status = "VISIBLE_UNVERIFIED";
                    }

                    alert.Opacity = Math.Max(alert.Opacity, promotedByMedia || alert.HasMedia ? 50 : 30);
                    break;

                default:
                    alert.RoutingDecision = "YELLOW";
                    if (alert.Status != "VISIBLE_VERIFIED" && alert.Status != "RESOLVED")
                    {
                        alert.Status = "PENDING_REVIEW";
                    }

                    alert.Opacity = 30;
                    break;
            }
        }

        private void ApplyLifecycleState(SecurityAlert alert, DateTime now, bool resetReviewDue = false)
        {
            var incidentTime = alert.IncidentTime == default ? now : alert.IncidentTime;
            var isHot = incidentTime >= now.AddMinutes(-30);
            var baseAutoHideAt = incidentTime.AddDays(7);

            alert.ReviewPriority = isHot ? "HOT" : "NORMAL";
            alert.AutoHideAt = baseAutoHideAt;
            alert.ExpiresAt = baseAutoHideAt;

            if (alert.Status == "PENDING_REVIEW")
            {
                if (resetReviewDue || !alert.ReviewDueAt.HasValue)
                {
                    alert.ReviewDueAt = now.AddMinutes(isHot ? 10 : 120);
                }
            }
            else
            {
                alert.ReviewDueAt = null;
            }

            if (_lifecycleRuntimeSettings.NeedsMoreInfoAutoHideEnabled && alert.Status == "NEEDS_MORE_INFO" && alert.MoreInfoDeadline.HasValue)
            {
                alert.AutoHideAt = alert.MoreInfoDeadline.Value < baseAutoHideAt
                    ? alert.MoreInfoDeadline.Value
                    : baseAutoHideAt;
                alert.ExpiresAt = alert.AutoHideAt;
            }

            if ((alert.Status == "VISIBLE_UNVERIFIED" || alert.Status == "VISIBLE_VERIFIED") && !alert.FirstVisibleAt.HasValue)
            {
                alert.FirstVisibleAt = now;
            }

            alert.DisplayPriority = alert.Status switch
            {
                "VISIBLE_VERIFIED" => isHot ? 100 : 80,
                "VISIBLE_UNVERIFIED" => isHot ? 90 : 70,
                "PENDING_REVIEW" when alert.ReviewDueAt.HasValue && alert.ReviewDueAt.Value < now => 20,
                "PENDING_REVIEW" => isHot ? 85 : 55,
                "RESOLVED" => 30,
                _ => 0
            };
        }

        private async Task<AlertEvaluationResult> EvaluateInitialAlertAsync(User user, AlertType alertType, string title, string description)
        {
            var recentReports = await _alertRepo.CountRecentReportsByUserAsync(user.Id, TimeSpan.FromMinutes(10));
            if (recentReports >= 3)
            {
                return new AlertEvaluationResult
                {
                    RoutingDecision = "RED",
                    FilterReason = "Bộ lọc tự động: gửi quá 3 báo cáo trong 10 phút",
                    TrustScoreBreakdown = "0: báo cáo bị chuyển luồng đỏ do tần suất gửi bất thường"
                };
            }

            if (ContainsOffensiveContent(title, description))
            {
                return new AlertEvaluationResult
                {
                    RoutingDecision = "RED",
                    FilterReason = "Bộ lọc tự động: nội dung có từ ngữ không phù hợp",
                    TrustScoreBreakdown = "0: báo cáo bị chuyển luồng đỏ do nội dung không phù hợp"
                };
            }

            if (LooksLikeSpam(title, description))
            {
                return new AlertEvaluationResult
                {
                    RoutingDecision = "RED",
                    FilterReason = "Bộ lọc tự động: nội dung có dấu hiệu spam hoặc không liên quan",
                    TrustScoreBreakdown = "0: báo cáo bị chuyển luồng đỏ do dấu hiệu spam"
                };
            }

            var now = DateTime.Now;
            var accountAgeDays = (now - user.CreatedAt).TotalDays;
            var rejectedReports = await _alertRepo.CountRejectedReportsByUserAsync(user.Id, TimeSpan.FromDays(30));
            var goodHistory = accountAgeDays > 30
                && user.ReputationScore >= 5
                && rejectedReports == 0
                && (!user.LockedUntil.HasValue || user.LockedUntil <= now);

            var trustScore = 0;
            var breakdown = new List<string>();
            var reasons = new List<string>();

            if (goodHistory)
            {
                trustScore += 20;
                breakdown.Add("+20: tài khoản trên 30 ngày và lịch sử tốt");
            }
            else if (accountAgeDays <= 30)
            {
                reasons.Add("Tài khoản chưa đủ 30 ngày để được cộng điểm uy tín");
            }
            else if (rejectedReports > 0 || user.ReputationScore < 5)
            {
                reasons.Add("Lịch sử báo cáo chưa đủ tốt để được cộng điểm uy tín");
            }

            var relatedKeywords = BuildRelatedKeywords(alertType);
            var hasRelevantKeywords = ContainsRelevantKeywords(title, description, relatedKeywords);
            if (description.Length >= 30 && hasRelevantKeywords)
            {
                trustScore += 10;
                breakdown.Add("+10: mô tả đủ rõ và có từ khóa liên quan");
            }
            else if (description.Length < 30)
            {
                reasons.Add("Mô tả dưới 30 ký tự nên chưa được cộng điểm rõ ràng");
            }
            else
            {
                reasons.Add("Mô tả chưa có đủ từ khóa liên quan đến loại sự cố");
            }

            if (accountAgeDays < 7)
            {
                trustScore -= 10;
                breakdown.Add("-10: tài khoản mới dưới 7 ngày");
                reasons.Add("Tài khoản mới dưới 7 ngày");
            }

            if (breakdown.Count == 0)
                breakdown.Add("0: chưa có yếu tố cộng hoặc trừ điểm ở bước đăng báo cáo");

            return new AlertEvaluationResult
            {
                TrustScore = trustScore,
                RoutingDecision = trustScore >= 10 ? "GREEN" : "YELLOW",
                FilterReason = reasons.Count == 0 ? "Bộ lọc tự động: đạt kiểm tra ban đầu" : string.Join("; ", reasons),
                TrustScoreBreakdown = string.Join(" | ", breakdown)
            };
        }

        private static bool ContainsOffensiveContent(string title, string description)
        {
            var normalized = NormalizeText($"{title} {description}");
            return OffensiveKeywords.Any(keyword => normalized.Contains(NormalizeText(keyword)));
        }

        private static bool LooksLikeSpam(string title, string description)
        {
            var text = NormalizeText($"{title} {description}");
            if (RepeatedCharacterRegex.IsMatch(text))
                return true;

            var urlCount = UrlRegex.Matches(text).Count;
            if (urlCount > 0)
                return true;

            if (SpamKeywords.Any(keyword => text.Contains(NormalizeText(keyword))))
                return true;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 6)
            {
                var distinctRatio = words.Distinct().Count() / (double)words.Length;
                if (distinctRatio < 0.45)
                    return true;
            }

            return false;
        }

        private static HashSet<string> BuildRelatedKeywords(AlertType alertType)
        {
            var rawKeywords = new[]
            {
                alertType.Name,
                alertType.Slug,
                alertType.Category?.Name,
                alertType.Category?.Slug,
                "mất", "trộm", "cuop", "cướp", "ẩu đả", "danh nhau", "đánh nhau",
                "lừa", "gia", "tai nan", "tai nạn", "xe", "du lich", "du lịch"
            };

            var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in rawKeywords.Where(v => !string.IsNullOrWhiteSpace(v)))
            {
                foreach (var token in NormalizeText(raw!).Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (token.Length >= 3)
                        keywords.Add(token);
                }
            }

            return keywords;
        }

        private static bool ContainsRelevantKeywords(string title, string description, HashSet<string> keywords)
        {
            if (keywords.Count == 0)
                return false;

            var haystack = NormalizeText($"{title} {description}");
            return keywords.Any(keyword => haystack.Contains(keyword));
        }

        private static string NormalizeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.Trim().ToLowerInvariant();
        }

        private static string AppendNote(string? existing, string note)
        {
            if (string.IsNullOrWhiteSpace(existing))
                return note;

            return existing.Contains(note, StringComparison.OrdinalIgnoreCase)
                ? existing
                : $"{existing}; {note}";
        }

        private static string AppendTrustBreakdown(string? existing, string note)
        {
            if (string.IsNullOrWhiteSpace(existing))
                return note;

            return existing.Contains(note, StringComparison.OrdinalIgnoreCase)
                ? existing
                : $"{existing} | {note}";
        }

        private static string ValidateMediaFile(string fileName, long fileSize)
        {
            if (fileSize <= 0)
                throw new Exception("File không hợp lệ");

            if (fileSize > MaxMediaFileSize)
                throw new Exception("File quá lớn. Kích thước tối đa là 500MB.");

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var allowedExtensions = AllowedImageExtensions.Concat(AllowedVideoExtensions);
            if (!allowedExtensions.Contains(extension))
                throw new Exception("Loại file không được hỗ trợ. Chỉ chấp nhận JPG, PNG, WEBP, MP4 hoặc WEBM.");

            return extension;
        }

        private string GetSessionDirectory(int alertId, string uploadId)
        {
            if (!Guid.TryParseExact(uploadId, "N", out _))
                throw new Exception("Phiên upload không hợp lệ");

            return Path.Combine(_env.ContentRootPath, "App_Data", "upload-sessions", "alerts", alertId.ToString(), uploadId);
        }

        private async Task<UploadSessionMetadata> LoadUploadSessionMetadataAsync(int alertId, int userId, string uploadId)
        {
            var sessionDir = GetSessionDirectory(alertId, uploadId);
            var metadataPath = Path.Combine(sessionDir, "metadata.json");
            if (!File.Exists(metadataPath))
                throw new Exception("Không tìm thấy phiên upload");

            var json = await File.ReadAllTextAsync(metadataPath);
            var metadata = JsonSerializer.Deserialize<UploadSessionMetadata>(json);
            if (metadata == null || metadata.AlertId != alertId || metadata.UserId != userId)
                throw new Exception("Phiên upload không hợp lệ");

            return metadata;
        }

        private async Task<AlertReport?> LoadAlertReportForReviewAsync(int reportId)
        {
            return await _context.AlertReports
                .Include(r => r.Alert)
                    .ThenInclude(a => a.User)
                .Include(r => r.Reporter)
                .FirstOrDefaultAsync(r => r.Id == reportId);
        }

        private static string? CleanOptionalNote(string? note)
        {
            return string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        }

        private static string GetAlertReportReasonLabel(string reason)
        {
            return reason switch
            {
                "THONG_TIN_SAI_SU_THAT" => "Thông tin sai sự thật",
                "NOI_DUNG_XUC_PHAM" => "Nội dung xúc phạm",
                "SPAM" => "Spam",
                "NGUOI_DUNG_GIA_MAO" => "Người dùng giả mạo",
                "KHAC" => "Khác",
                _ => reason
            };
        }

        private static void EnsureUserCanSubmit(User user)
        {
            if (!user.IsActive || user.IsBanned)
                throw new Exception("Tài khoản của bạn hiện không thể thực hiện thao tác này");

            if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.Now)
                throw new Exception($"Tài khoản bị khóa đến {user.LockedUntil:dd/MM/yyyy HH:mm}. Vui lòng chờ hết thời gian phạt.");
        }

        private async Task CreateModerationLogAsync(SecurityAlert alert, int adminUserId, string actionType, string previousStatus, string? reason, string? detail = null)
        {
            _context.ModerationLogs.Add(new ModerationLog
            {
                AlertId = alert.Id,
                AdminUserId = adminUserId,
                ActionType = actionType,
                PreviousStatus = previousStatus,
                NewStatus = alert.Status,
                Reason = reason,
                Detail = detail,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        private async Task CreateNotificationAsync(int userId, int? alertId, string notificationType, string title, string message)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                AlertId = alertId,
                NotificationType = notificationType,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        private async Task CreateNotificationsForAdminsAsync(int? alertId, string title, string message, string notificationType)
        {
            var adminIds = await _context.Users
                .Where(u => u.Role == "Admin" && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            if (adminIds.Count == 0)
                return;

            foreach (var adminId in adminIds)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = adminId,
                    AlertId = alertId,
                    NotificationType = notificationType,
                    Title = title,
                    Message = message,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task<AlertAppeal?> GetLatestAppealAsync(int alertId)
        {
            return await _context.AlertAppeals
                .Where(a => a.AlertId == alertId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();
        }

        private static bool CanUserAppeal(SecurityAlert alert, AlertAppeal? latestAppeal)
        {
            return alert.Status == "REJECTED"
                && latestAppeal == null
                && DateTime.Now <= alert.UpdatedAt.AddHours(24);
        }

        /// <summary>Chuyển Entity → DTO cho frontend</summary>
        private static AlertMapDto MapToDto(SecurityAlert a)
        {
            var latestAppeal = a.Appeals?
                .OrderByDescending(ap => ap.CreatedAt)
                .FirstOrDefault();

            return new AlertMapDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                Title = a.Title,
                Description = a.Description,
                AddressText = a.AddressText,
                IncidentTime = a.IncidentTime,
                AlertTypeId = a.AlertTypeId,
                AlertTypeName = a.AlertType?.Name ?? "",
                AlertTypeSlug = a.AlertType?.Slug ?? "",
                IconEmoji = a.AlertType?.IconEmoji,
                IconUrl = a.AlertType?.IconUrl,
                CategoryName = a.AlertType?.Category?.Name ?? "",
                CategoryColor = a.AlertType?.Category?.ColorHex ?? "#666",
                Status = a.Status,
                RoutingDecision = a.RoutingDecision,
                ModerationReason = a.ModerationReason,
                FilterReason = a.FilterReason,
                MoreInfoDeadline = a.MoreInfoDeadline,
                ReviewPriority = a.ReviewPriority,
                ReviewDueAt = a.ReviewDueAt,
                DisplayPriority = a.DisplayPriority,
                FirstVisibleAt = a.FirstVisibleAt,
                AutoHideAt = a.AutoHideAt,
                IsReviewOverdue = a.Status == "PENDING_REVIEW" && a.ReviewDueAt.HasValue && a.ReviewDueAt.Value < DateTime.Now,
                Opacity = a.Opacity,
                ConfirmCount = a.ConfirmCount,
                DenyCount = a.DenyCount,
                HasMedia = a.HasMedia,
                TrustScore = a.TrustScore,
                CanAppeal = a.Status == "REJECTED"
                    && latestAppeal == null
                    && DateTime.Now <= a.UpdatedAt.AddHours(24),
                AppealStatus = latestAppeal?.Status,
                AppealReason = latestAppeal?.Reason,
                AppealSubmittedAt = latestAppeal?.CreatedAt,
                AppealReviewNote = latestAppeal?.ReviewNote,
                MediaUrls = a.Media?.Where(m => m.IsActive)
                    .Select(m => m.FilePath).ToList() ?? new(),
                UserName = a.User?.FullName ?? "Ẩn danh",
                UserReputationScore = a.User?.ReputationScore ?? 5,
                CreatedAt = a.CreatedAt
            };
        }

        // ═══════════════════════════════════════════════
        // MY REPORTS
        // ═══════════════════════════════════════════════

        public async Task<List<AlertMapDto>> GetMyAlertsAsync(int userId)
        {
            var alerts = await _alertRepo.GetAlertsByUserAsync(userId);
            return alerts.Select(MapToDto).ToList();
        }

        public async Task<(bool Success, string Message)> SubmitAlertReportAsync(int alertId, int reporterId, string reason, string? description)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == reporterId);
            if (user == null)
                return (false, "Không tìm thấy người dùng");

            if (!user.IsActive || user.IsBanned)
                return (false, "Tài khoản của bạn hiện không thể thực hiện thao tác này");

            if (string.IsNullOrWhiteSpace(reason))
                return (false, "Vui lòng chọn lý do báo cáo");

            var normalizedReason = reason.Trim().ToUpperInvariant();
            if (!AllowedAlertReportReasons.Contains(normalizedReason))
                return (false, "Lý do báo cáo không hợp lệ");

            var alert = await _context.SecurityAlerts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == alertId);
            if (alert == null || alert.Status == "DELETED")
                return (false, "Không tìm thấy bài viết");

            var existing = await _context.AlertReports
                .FirstOrDefaultAsync(r => r.AlertId == alertId && r.ReporterId == reporterId);
            if (existing != null)
                return (false, "Bạn đã báo cáo bài viết này");

            var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            if (trimmedDescription != null && trimmedDescription.Length > 300)
                return (false, "Mô tả thêm tối đa 300 ký tự");

            _context.AlertReports.Add(new AlertReport
            {
                AlertId = alertId,
                ReporterId = reporterId,
                Reason = normalizedReason,
                Description = trimmedDescription,
                Status = "PENDING",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return (true, "Đã gửi báo cáo vi phạm");
        }

        // ═══════════════════════════════════════════════
        // ADMIN: DUYỆT BÁO CÁO
        // ═══════════════════════════════════════════════

        public async Task<List<AlertMapDto>> GetPendingAlertsAsync()
        {
            var alerts = await _alertRepo.GetPendingAlertsAsync(); // Sẽ fetch VISIBLE_UNVERIFIED
            return alerts.Select(MapToDto).ToList();
        }

        public async Task<(List<AlertMapDto> Items, int Total)> GetAllAlertsForAdminAsync(
            string? status, int page, int pageSize)
        {
            var alerts = await _alertRepo.GetAllAlertsForAdminAsync(status, page, pageSize);
            var total = await _alertRepo.CountAllAlertsAsync(status);
            return (alerts.Select(MapToDto).ToList(), total);
        }

        public async Task<List<object>> GetAlertReportsForAdminAsync()
        {
            var reports = await _context.AlertReports
                .Include(r => r.Alert)
                    .ThenInclude(a => a.User)
                .Include(r => r.Reporter)
                .OrderBy(r => r.Status == "PENDING" ? 0 : 1)
                .ThenByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    reportId = r.Id,
                    alertId = r.AlertId,
                    alertTitle = r.Alert.Title,
                    alertStatus = r.Alert.Status,
                    reporterName = r.Reporter.FullName,
                    reporterId = r.ReporterId,
                    reason = r.Reason,
                    reasonLabel = GetAlertReportReasonLabel(r.Reason),
                    description = r.Description,
                    createdAt = r.CreatedAt,
                    status = r.Status,
                    ownerName = r.Alert.User.FullName,
                    latitude = r.Alert.Latitude,
                    longitude = r.Alert.Longitude
                })
                .ToListAsync();

            return reports.Cast<object>().ToList();
        }

        public async Task<(bool Success, string Message)> HideAlertFromReportAsync(int reportId, int adminId, string? note)
        {
            var report = await LoadAlertReportForReviewAsync(reportId);
            if (report == null)
                return (false, "Không tìm thấy báo cáo vi phạm");

            if (report.Status == "REVIEWED")
                return (false, "Báo cáo vi phạm này đã được xử lý");

            var alert = report.Alert;
            var previousStatus = alert.Status;
            alert.Status = "HIDDEN";
            alert.Opacity = 0;
            alert.ModerationReason = CleanOptionalNote(note);
            alert.UpdatedAt = DateTime.Now;
            ApplyLifecycleState(alert, alert.UpdatedAt);
            report.Status = "REVIEWED";

            await _context.SaveChangesAsync();
            await CreateModerationLogAsync(alert, adminId, "HIDE", previousStatus, CleanOptionalNote(note), "Ẩn bài từ báo cáo vi phạm");
            return (true, "Đã ẩn bài viết vi phạm");
        }

        public async Task<(bool Success, string Message)> DeleteAlertFromReportAsync(int reportId, int adminId, string? note)
        {
            var report = await LoadAlertReportForReviewAsync(reportId);
            if (report == null)
                return (false, "Không tìm thấy báo cáo vi phạm");

            if (report.Status == "REVIEWED")
                return (false, "Báo cáo vi phạm này đã được xử lý");

            var alert = report.Alert;
            var previousStatus = alert.Status;
            alert.Status = "DELETED";
            alert.Opacity = 0;
            alert.ModerationReason = CleanOptionalNote(note);
            alert.UpdatedAt = DateTime.Now;
            ApplyLifecycleState(alert, alert.UpdatedAt);
            report.Status = "REVIEWED";

            await _context.SaveChangesAsync();
            await CreateModerationLogAsync(alert, adminId, "DELETE_SOFT", previousStatus, CleanOptionalNote(note), "Xóa mềm bài viết từ báo cáo vi phạm");
            return (true, "Đã xóa bài viết vi phạm");
        }

        public async Task<(bool Success, string Message)> BanUserFromReportAsync(int reportId, int adminId, string? note)
        {
            var report = await LoadAlertReportForReviewAsync(reportId);
            if (report == null)
                return (false, "Không tìm thấy báo cáo vi phạm");

            if (report.Status == "REVIEWED")
                return (false, "Báo cáo vi phạm này đã được xử lý");

            var owner = report.Alert.User;
            if (owner == null)
                return (false, "Không tìm thấy chủ bài viết");

            owner.IsBanned = true;
            owner.IsActive = false;

            var affectedAlerts = await _context.SecurityAlerts
                .Where(a => a.UserId == owner.Id && a.Status != "DELETED")
                .ToListAsync();

            foreach (var alert in affectedAlerts)
            {
                var previousStatus = alert.Status;
                alert.Status = "HIDDEN";
                alert.Opacity = 0;
                alert.UpdatedAt = DateTime.Now;
                alert.ModerationReason = CleanOptionalNote(note) ?? "Tài khoản bị khóa do vi phạm";
                ApplyLifecycleState(alert, alert.UpdatedAt);
                await CreateModerationLogAsync(alert, adminId, "HIDE_BY_BAN", previousStatus, CleanOptionalNote(note), "Ẩn bài do khóa tài khoản người đăng");
            }

            report.Status = "REVIEWED";
            await _context.SaveChangesAsync();
            await CreateModerationLogAsync(report.Alert, adminId, "BAN_USER", report.Alert.Status, CleanOptionalNote(note), "Khóa tài khoản người đăng từ báo cáo vi phạm");
            await CreateNotificationAsync(owner.Id, report.Alert.Id, "USER_BANNED", "Tài khoản đã bị khóa", "Tài khoản của bạn đã bị khóa do vi phạm nội dung cộng đồng.");
            return (true, "Đã khóa tài khoản và ẩn toàn bộ bài viết của người dùng");
        }

        public async Task<(bool Success, string Message)> IgnoreAlertReportAsync(int reportId, int adminId, string? note)
        {
            var report = await LoadAlertReportForReviewAsync(reportId);
            if (report == null)
                return (false, "Không tìm thấy báo cáo vi phạm");

            if (report.Status == "REVIEWED")
                return (false, "Báo cáo vi phạm này đã được xử lý");

            report.Status = "REVIEWED";
            await _context.SaveChangesAsync();
            await CreateModerationLogAsync(report.Alert, adminId, "IGNORE_REPORT", report.Alert.Status, CleanOptionalNote(note), "Đã xem và bỏ qua báo cáo vi phạm");
            return (true, "Đã đánh dấu báo cáo vi phạm là đã xem");
        }

        /// <summary>
        /// Admin APPROVE: chuyển sang VISIBLE_VERIFIED → Xác nhận cứng.
        /// </summary>
        public async Task<bool> ApproveAlertAsync(int alertId, int adminUserId)
        {
            var alert = await _context.SecurityAlerts.FirstOrDefaultAsync(a => a.Id == alertId);
            if (alert == null) return false;

            var previousStatus = alert.Status;
            alert.Status = "VISIBLE_VERIFIED";
            alert.Opacity = 100;
            alert.ModerationReason = null;
            alert.MoreInfoDeadline = null;
            alert.UpdatedAt = DateTime.Now;
            ApplyLifecycleState(alert, alert.UpdatedAt);

            var reporter = await _context.Users.FirstOrDefaultAsync(u => u.Id == alert.UserId);
            if (reporter != null)
            {
                reporter.ReputationScore = Math.Min(10, reporter.ReputationScore + 5);
            }

            await _context.SaveChangesAsync();
            await CreateModerationLogAsync(alert, adminUserId, "APPROVE", previousStatus, null, "Duyệt hiển thị công khai trên bản đồ");
            await CreateNotificationAsync(alert.UserId, alert.Id, "ALERT_APPROVED", "Báo cáo đã được duyệt", "Báo cáo của bạn đã được admin duyệt và hiển thị ở trạng thái đã xác thực.");
            return true;
        }

        /// <summary>
        /// Admin REJECT: ẩn alert, lưu lý do.
        /// </summary>
        public async Task<bool> RejectAlertAsync(int alertId, int adminUserId, string reason)
        {
            var alert = await _context.SecurityAlerts.FirstOrDefaultAsync(a => a.Id == alertId);
            if (alert == null) return false;

            var previousStatus = alert.Status;
            alert.Status = "REJECTED";
            alert.ModerationReason = reason;
            alert.MoreInfoDeadline = null;
            alert.Opacity = 0;
            alert.UpdatedAt = DateTime.Now;
            ApplyLifecycleState(alert, alert.UpdatedAt);

            var reporter = await _context.Users.FirstOrDefaultAsync(u => u.Id == alert.UserId);
            if (reporter != null)
            {
                reporter.ReputationScore -= 10;
                if (reporter.ReputationScore <= 0)
                {
                    reporter.LockedUntil = DateTime.Now.AddDays(7);
                    reporter.ReputationScore = 1;
                }
            }

            await _context.SaveChangesAsync();
            await CreateModerationLogAsync(alert, adminUserId, "REJECT", previousStatus, reason, "Bác bỏ do fake hoặc spam");
            await CreateNotificationAsync(alert.UserId, alert.Id, "ALERT_REJECTED", "Báo cáo bị bác bỏ", $"Báo cáo của bạn đã bị bác bỏ. Lý do: {reason}. Bạn có thể khiếu nại trong 24 giờ.");
            return true;
        }

        public async Task<bool> MarkInsufficientAsync(int alertId, int adminId, string reason)
        {
            var alert = await _context.SecurityAlerts.FirstOrDefaultAsync(a => a.Id == alertId);
            if (alert == null) return false;

            var previousStatus = alert.Status;
            alert.Status = "NOT_ENOUGH_EVIDENCE";
            alert.ModerationReason = reason;
            alert.MoreInfoDeadline = null;
            alert.Opacity = 0;
            alert.UpdatedAt = DateTime.Now;
            ApplyLifecycleState(alert, alert.UpdatedAt);

            await _context.SaveChangesAsync();
            await CreateModerationLogAsync(alert, adminId, "INSUFFICIENT", previousStatus, reason, "Ẩn báo cáo nhưng không phạt người dùng");
            await CreateNotificationAsync(alert.UserId, alert.Id, "ALERT_INSUFFICIENT", "Báo cáo chưa đủ cơ sở", $"Báo cáo của bạn hiện chưa đủ cơ sở để hiển thị. Lý do: {reason}.");
            return true;
        }

        public async Task<bool> RequestMoreInfoAsync(int alertId, int adminId, string reason)
        {
            var alert = await _context.SecurityAlerts.FirstOrDefaultAsync(a => a.Id == alertId);
            if (alert == null) return false;

            var autoHideEnabled = GetNeedsMoreInfoAutoHideEnabled();
            var previousStatus = alert.Status;
            alert.Status = "NEEDS_MORE_INFO";
            alert.ModerationReason = reason;
            alert.MoreInfoDeadline = autoHideEnabled ? DateTime.Now.AddHours(24) : null;
            alert.Opacity = 0;
            alert.UpdatedAt = DateTime.Now;
            ApplyLifecycleState(alert, alert.UpdatedAt);

            await _context.SaveChangesAsync();
            await CreateModerationLogAsync(
                alert,
                adminId,
                "REQUEST_MORE_INFO",
                previousStatus,
                reason,
                autoHideEnabled
                    ? "Yêu cầu người dùng bổ sung bằng chứng trong 24 giờ"
                    : "Yêu cầu người dùng bổ sung bằng chứng mà không tự ẩn sau 24 giờ");
            await CreateNotificationAsync(
                alert.UserId,
                alert.Id,
                "ALERT_NEEDS_MORE_INFO",
                "Cần bổ sung bằng chứng",
                autoHideEnabled
                    ? $"Admin yêu cầu bạn bổ sung thêm bằng chứng trong 24 giờ. Lý do: {reason}."
                    : $"Admin yêu cầu bạn bổ sung thêm bằng chứng. Báo cáo hiện không tự ẩn sau 24 giờ. Lý do: {reason}.");
            return true;
        }

        public async Task<bool> ResolveAlertAsync(int alertId, int userId)
        {
            var alert = await _context.SecurityAlerts.FirstOrDefaultAsync(a => a.Id == alertId);
            if (alert == null) return false;

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null || !user.IsActive || user.IsBanned)
                throw new Exception("Tài khoản của bạn hiện không thể thực hiện thao tác này");

            bool isOwner = alert.UserId == userId;
            bool isAdmin = user.Role == "Admin";
            if (!isOwner && !isAdmin) return false;

            var previousStatus = alert.Status;
            alert.Status = "RESOLVED";
            alert.ResolvedAt = DateTime.Now;
            alert.UpdatedAt = DateTime.Now;
            ApplyLifecycleState(alert, alert.UpdatedAt);
            await _context.SaveChangesAsync();

            if (isAdmin)
            {
                await CreateModerationLogAsync(alert, userId, "RESOLVE", previousStatus, null, "Admin đánh dấu sự cố đã xử lý");
            }

            return true;
        }

        public async Task<(bool Success, string Message)> SubmitAppealAsync(int alertId, int userId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return (false, "Vui lòng nhập lý do khiếu nại");

            var alert = await _context.SecurityAlerts.FirstOrDefaultAsync(a => a.Id == alertId);
            if (alert == null)
                return (false, "Không tìm thấy báo cáo");

            if (alert.UserId != userId)
                return (false, "Bạn chỉ có thể khiếu nại báo cáo của chính mình");

            if (alert.Status != "REJECTED")
                return (false, "Chỉ báo cáo bị bác bỏ mới có thể khiếu nại");

            var latestAppeal = await GetLatestAppealAsync(alertId);
            if (latestAppeal != null)
                return (false, "Báo cáo này đã có đơn khiếu nại trước đó");

            if (DateTime.Now > alert.UpdatedAt.AddHours(24))
                return (false, "Đã quá thời hạn 24 giờ để khiếu nại");

            _context.AlertAppeals.Add(new AlertAppeal
            {
                AlertId = alertId,
                UserId = userId,
                Reason = reason.Trim(),
                Status = "PENDING",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await CreateNotificationsForAdminsAsync(alert.Id, "Có đơn khiếu nại mới", $"Báo cáo #{alert.Id} vừa được người dùng khiếu nại và cần admin cấp cao xem lại.", "ALERT_APPEAL_PENDING");
            await CreateNotificationAsync(userId, alert.Id, "ALERT_APPEAL_SUBMITTED", "Đã gửi khiếu nại", "Đơn khiếu nại của bạn đã được ghi nhận và đang chờ admin xem xét.");
            return (true, "Đã gửi khiếu nại thành công");
        }

        public async Task<(bool Success, string Message)> ApproveAppealAsync(int alertId, int adminId, string? reviewNote)
        {
            var alert = await _context.SecurityAlerts.FirstOrDefaultAsync(a => a.Id == alertId);
            if (alert == null)
                return (false, "Không tìm thấy báo cáo");

            var appeal = await GetLatestAppealAsync(alertId);
            if (appeal == null || appeal.Status != "PENDING")
                return (false, "Không có đơn khiếu nại đang chờ xử lý");

            var previousStatus = alert.Status;
            appeal.Status = "APPROVED";
            appeal.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? "Chấp nhận khiếu nại và đưa báo cáo về hàng chờ duyệt lại." : reviewNote.Trim();
            appeal.ReviewedByAdminId = adminId;
            appeal.ReviewedAt = DateTime.Now;

            alert.Status = "PENDING_REVIEW";
            alert.RoutingDecision = "YELLOW";
            alert.Opacity = 30;
            alert.ModerationReason = null;
            alert.MoreInfoDeadline = null;
            alert.UpdatedAt = DateTime.Now;
            ApplyLifecycleState(alert, alert.UpdatedAt, resetReviewDue: true);

            await _context.SaveChangesAsync();
            await CreateModerationLogAsync(alert, adminId, "APPEAL_APPROVED", previousStatus, appeal.Reason, appeal.ReviewNote);
            await CreateNotificationAsync(alert.UserId, alert.Id, "ALERT_APPEAL_APPROVED", "Khiếu nại được chấp nhận", "Khiếu nại của bạn đã được chấp nhận. Báo cáo được đưa về hàng chờ kiểm duyệt lại.");
            return (true, "Đã chấp nhận khiếu nại và mở lại báo cáo để duyệt lại");
        }

        public async Task<(bool Success, string Message)> RejectAppealAsync(int alertId, int adminId, string? reviewNote)
        {
            var alert = await _context.SecurityAlerts.FirstOrDefaultAsync(a => a.Id == alertId);
            if (alert == null)
                return (false, "Không tìm thấy báo cáo");

            var appeal = await GetLatestAppealAsync(alertId);
            if (appeal == null || appeal.Status != "PENDING")
                return (false, "Không có đơn khiếu nại đang chờ xử lý");

            appeal.Status = "REJECTED";
            appeal.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? "Giữ nguyên quyết định bác bỏ trước đó." : reviewNote.Trim();
            appeal.ReviewedByAdminId = adminId;
            appeal.ReviewedAt = DateTime.Now;
            alert.UpdatedAt = DateTime.Now;
            ApplyLifecycleState(alert, alert.UpdatedAt);

            await _context.SaveChangesAsync();
            await CreateModerationLogAsync(alert, adminId, "APPEAL_REJECTED", alert.Status, appeal.Reason, appeal.ReviewNote);
            await CreateNotificationAsync(alert.UserId, alert.Id, "ALERT_APPEAL_REJECTED", "Khiếu nại không được chấp nhận", $"Admin đã giữ nguyên quyết định bác bỏ báo cáo. Ghi chú: {appeal.ReviewNote}");
            return (true, "Đã giữ nguyên quyết định bác bỏ sau khi xem khiếu nại");
        }
        // ═══════════════════════════════════════════════
        // COMMENTS
        // ═══════════════════════════════════════════════

        public async Task<List<object>> GetCommentsAsync(int alertId)
        {
            var comments = await _context.AlertComments
                .Where(c => c.AlertId == alertId)
                .OrderByDescending(c => c.CreatedAt)
                .Take(50)
                .Include(c => c.User)
                .Select(c => new
                {
                    id = c.Id,
                    alertId = c.AlertId,
                    userId = c.UserId,
                    userName = c.User.FullName ?? "Ẩn danh",
                    userReputation = c.User.ReputationScore,
                    content = c.Content,
                    mediaUrl = c.MediaUrl,
                    createdAt = c.CreatedAt
                })
                .ToListAsync();

            return comments.Cast<object>().ToList();
        }

        public async Task<object> AddCommentAsync(int alertId, int userId, string content, IFormFile? mediaFile)
        {
            var alert = await _alertRepo.GetByIdAsync(alertId);
            if (alert == null) throw new Exception("Không tìm thấy báo cáo");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new Exception("Không tìm thấy người dùng");
            if (!user.IsActive || user.IsBanned) throw new Exception("Tài khoản của bạn hiện không thể thực hiện thao tác này");

            // Rate limit: tối đa 5 bình luận/giờ
            var oneHourAgo = DateTime.Now.AddHours(-1);
            var recentCount = await _context.AlertComments
                .CountAsync(c => c.AlertId == alertId && c.UserId == userId && c.CreatedAt >= oneHourAgo);
            if (recentCount >= 5)
                throw new Exception("Bạn đã bình luận quá nhiều. Vui lòng thử lại sau.");

            string? mediaUrl = null;
            if (mediaFile != null && mediaFile.Length > 0)
            {
                var uploads = Path.Combine(_env.ContentRootPath, "App_Data", "uploads", "comments");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(mediaFile.FileName);
                var filePath = Path.Combine(uploads, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await mediaFile.CopyToAsync(stream);
                }
                mediaUrl = "/uploads/comments/" + fileName;
            }

            var comment = new AlertComment
            {
                AlertId = alertId,
                UserId = userId,
                Content = content,
                MediaUrl = mediaUrl,
                CreatedAt = DateTime.Now
            };

            _context.AlertComments.Add(comment);
            await _context.SaveChangesAsync();

            return new
            {
                id = comment.Id,
                alertId = comment.AlertId,
                userId = comment.UserId,
                userName = user.FullName ?? "Ẩn danh",
                userReputation = user.ReputationScore,
                content = comment.Content,
                mediaUrl = comment.MediaUrl,
                createdAt = comment.CreatedAt
            };
        }

        private sealed class AlertEvaluationResult
        {
            public int TrustScore { get; set; }
            public string RoutingDecision { get; set; } = "YELLOW";
            public string? FilterReason { get; set; }
            public string? TrustScoreBreakdown { get; set; }
        }

        private sealed class UploadSessionMetadata
        {
            public int AlertId { get; set; }
            public int UserId { get; set; }
            public string FileName { get; set; } = string.Empty;
            public string Extension { get; set; } = string.Empty;
            public long FileSize { get; set; }
            public string? ContentType { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}
