using Microsoft.EntityFrameworkCore;
using DaNangSafeMap.Data;
using DaNangSafeMap.Models.Entities;

namespace DaNangSafeMap.Repositories
{
    /// <summary>
    /// Thực thi các thao tác với SecurityAlerts và bảng liên quan.
    /// </summary>
    public class AlertRepository : IAlertRepository
    {
        private readonly ApplicationDbContext _context;

        public AlertRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // ═══════════════════════════════════════════════
        // ALERT TYPES
        // ═══════════════════════════════════════════════

        public async Task<List<AlertType>> GetAlertTypesAsync()
        {
            return await _context.AlertTypes
                .Include(t => t.Category)
                .Where(t => t.IsActive && t.Category.IsActive)
                .OrderBy(t => t.SortOrder)
                .ToListAsync();
        }

        // ═══════════════════════════════════════════════
        // CRUD ALERTS
        // ═══════════════════════════════════════════════

        public async Task<SecurityAlert> CreateAlertAsync(SecurityAlert alert)
        {
            _context.SecurityAlerts.Add(alert);
            await _context.SaveChangesAsync();
            return alert;
        }

        public async Task<SecurityAlert?> GetByIdAsync(int id)
        {
            return await _context.SecurityAlerts.FindAsync(id);
        }

        public async Task<SecurityAlert?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.SecurityAlerts
                .Include(a => a.AlertType)
                    .ThenInclude(t => t.Category)
                .Include(a => a.User)
                .Include(a => a.Media.Where(m => m.IsActive))
                .Include(a => a.Verifications)
                .Include(a => a.Appeals)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<AlertType?> GetAlertTypeByIdAsync(int id)
        {
            return await _context.AlertTypes
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id && t.IsActive && t.Category.IsActive);
        }

        public async Task UpdateAlertAsync(SecurityAlert alert)
        {
            _context.SecurityAlerts.Update(alert);
            await _context.SaveChangesAsync();
        }

        // ═══════════════════════════════════════════════
        // MAP QUERIES
        // ═══════════════════════════════════════════════

        public async Task<List<SecurityAlert>> GetAlertsForMapAsync(
            decimal southLat, decimal northLat,
            decimal westLng, decimal eastLng,
            DateTime fromTime, DateTime toTime,
            bool includeHidden = false)
        {
            // Fix timezone discrepancy: JS .toISOString() is UTC, but DB timestamps are local (GMT+7)
            var from = fromTime.Kind == DateTimeKind.Utc ? fromTime.ToLocalTime() : fromTime;
            var to = toTime.Kind == DateTimeKind.Utc ? toTime.ToLocalTime() : toTime;

            return await _context.SecurityAlerts
                .Include(a => a.AlertType)
                    .ThenInclude(t => t.Category)
                .Include(a => a.User)
                .Include(a => a.Media.Where(m => m.IsActive))
                .Where(a =>
                    a.Latitude >= southLat && a.Latitude <= northLat &&
                    a.Longitude >= westLng && a.Longitude <= eastLng &&
                    (
                        (a.Status == "VISIBLE_VERIFIED" && a.IncidentTime >= from) ||
                        (a.Status == "VISIBLE_UNVERIFIED" && a.IncidentTime >= from) ||
                        (a.Status == "RESOLVED" && a.IncidentTime >= from) ||
                        (includeHidden && a.Status == "HIDDEN" && a.IncidentTime >= from) ||
                        (includeHidden && a.Status == "NEEDS_MORE_INFO" && a.IncidentTime >= from) ||
                        (includeHidden && a.Status == "NOT_ENOUGH_EVIDENCE" && a.IncidentTime >= from) ||
                        (includeHidden && a.Status == "REJECTED" && a.IncidentTime >= from) ||
                        (includeHidden && a.Status == "EXPIRED" && a.IncidentTime >= from)
                    )
                )
                .OrderByDescending(a => a.DisplayPriority)
                .ThenByDescending(a => a.CreatedAt)
                .Take(500)
                .ToListAsync();
        }

        public async Task<List<SecurityAlert>> GetHeatmapDataAsync(
            DateTime fromTime, DateTime toTime)
        {
            var from = fromTime.Kind == DateTimeKind.Utc ? fromTime.ToLocalTime() : fromTime;

            return await _context.SecurityAlerts
                .Where(a =>
                    (a.Status == "VISIBLE_VERIFIED" && a.IncidentTime >= from) ||
                    (a.Status == "VISIBLE_UNVERIFIED" && a.IncidentTime >= from)
                )
                .Select(a => new SecurityAlert
                {
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    TrustScore = a.TrustScore,
                    ConfirmCount = a.ConfirmCount
                })
                .ToListAsync();
        }

        // ═══════════════════════════════════════════════
        // MEDIA
        // ═══════════════════════════════════════════════

        public async Task<AlertMedia> AddMediaAsync(AlertMedia media)
        {
            _context.AlertMedia.Add(media);
            await _context.SaveChangesAsync();
            return media;
        }

        // ═══════════════════════════════════════════════
        // VERIFICATIONS
        // ═══════════════════════════════════════════════

        public async Task<AlertVerification?> GetVerificationAsync(int alertId, int userId)
        {
            return await _context.AlertVerifications
                .FirstOrDefaultAsync(v => v.AlertId == alertId && v.UserId == userId);
        }

        public async Task AddVerificationAsync(AlertVerification verification)
        {
            _context.AlertVerifications.Add(verification);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateVerificationAsync(AlertVerification verification)
        {
            _context.AlertVerifications.Update(verification);
            await _context.SaveChangesAsync();
        }

        // ═══════════════════════════════════════════════
        // AUTO-EXPIRE
        // ═══════════════════════════════════════════════

        public async Task<List<SecurityAlert>> GetExpiredAlertsAsync(bool includeNeedsMoreInfoDeadlineExpiration)
        {
            var now = DateTime.Now;
            return await _context.SecurityAlerts
                .Where(a => a.Status != "EXPIRED")
                .Where(a =>
                    (a.AutoHideAt != null && a.AutoHideAt <= now)
                    ||
                    (includeNeedsMoreInfoDeadlineExpiration && a.Status == "NEEDS_MORE_INFO" && a.MoreInfoDeadline != null && a.MoreInfoDeadline <= now)
                    ||
                    (a.AutoHideAt == null && a.ExpiresAt != null && a.ExpiresAt <= now)
                )
                .ToListAsync();
        }

        // ═══════════════════════════════════════════════
        // CLUSTERING CHECK
        // ═══════════════════════════════════════════════

        public async Task<int> CountNearbyAlertsAsync(
            decimal lat, decimal lng, int alertTypeId, int withinMinutes)
        {
            var since = DateTime.Now.AddMinutes(-withinMinutes);
            // ~200m ≈ 0.0018 degrees
            const decimal radius = 0.0018m;

            return await _context.SecurityAlerts
                .Where(a => a.AlertTypeId == alertTypeId)
                .Where(a => a.Status != "REJECTED" && a.Status != "EXPIRED" && a.Status != "HIDDEN" && a.Status != "DELETED")
                .Where(a => Math.Abs(a.Latitude - lat) < radius)
                .Where(a => Math.Abs(a.Longitude - lng) < radius)
                .Where(a => a.CreatedAt >= since)
                .CountAsync();
        }

        public async Task<int> CountRecentReportsByUserAsync(int userId, TimeSpan within)
        {
            var since = DateTime.Now.Subtract(within);
            return await _context.SecurityAlerts
                .Where(a => a.UserId == userId && a.CreatedAt >= since)
                .CountAsync();
        }

        public async Task<int> CountRejectedReportsByUserAsync(int userId, TimeSpan within)
        {
            var since = DateTime.Now.Subtract(within);
            return await _context.SecurityAlerts
                .Where(a => a.UserId == userId)
                .Where(a => a.Status == "REJECTED")
                .Where(a => a.CreatedAt >= since)
                .CountAsync();
        }
        // ═══════════════════════════════════════════════
        // MY REPORTS
        // ═══════════════════════════════════════════════

        public async Task<List<SecurityAlert>> GetAlertsByUserAsync(int userId)
        {
            return await _context.SecurityAlerts
                .Include(a => a.AlertType)
                    .ThenInclude(t => t.Category)
                .Include(a => a.Media.Where(m => m.IsActive))
                .Include(a => a.Appeals)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        // ═══════════════════════════════════════════════
        // ADMIN MANAGEMENT
        // ═══════════════════════════════════════════════

        public async Task<List<SecurityAlert>> GetPendingAlertsAsync()
        {
            var now = DateTime.Now;
            return await _context.SecurityAlerts
                .Include(a => a.AlertType)
                    .ThenInclude(t => t.Category)
                .Include(a => a.User)
                .Include(a => a.Media.Where(m => m.IsActive))
                .Include(a => a.Appeals)
                .Where(a => a.Status == "PENDING_REVIEW")
                .OrderBy(a => a.ReviewPriority == "HOT" && (a.ReviewDueAt == null || a.ReviewDueAt >= now) ? 0 : a.ReviewDueAt != null && a.ReviewDueAt < now ? 2 : 1)
                .ThenBy(a => a.ReviewDueAt)
                .ThenByDescending(a => a.DisplayPriority)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<SecurityAlert>> GetAllAlertsForAdminAsync(
            string? status, int page, int pageSize)
        {
            var query = _context.SecurityAlerts
                .Include(a => a.AlertType)
                    .ThenInclude(t => t.Category)
                .Include(a => a.User)
                .Include(a => a.Media.Where(m => m.IsActive))
                .Include(a => a.Appeals)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);

            var now = DateTime.Now;

            return await query
                .OrderBy(a => a.Status == "PENDING_REVIEW" ? (a.ReviewPriority == "HOT" && (a.ReviewDueAt == null || a.ReviewDueAt >= now) ? 0 : a.ReviewDueAt != null && a.ReviewDueAt < now ? 2 : 1) : 3)
                .ThenBy(a => a.Status == "PENDING_REVIEW" ? a.ReviewDueAt : null)
                .ThenByDescending(a => a.DisplayPriority)
                .ThenByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAllAlertsAsync(string? status)
        {
            var query = _context.SecurityAlerts.AsQueryable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);
            return await query.CountAsync();
        }
    }
}
