using DaNangSafeMap.Data;
using DaNangSafeMap.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DaNangSafeMap.Services.Implementations
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _db;
        public NotificationService(ApplicationDbContext db) => _db = db;

        // Tự đảm bảo bảng Notifications tồn tại (chạy 1 lần duy nhất)
        private static bool _tableChecked = false;
        private async Task EnsureTableAsync()
        {
            if (_tableChecked) return;
            await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(
                _db.Database,
                @"CREATE TABLE IF NOT EXISTS `Notifications` (
                    `Id`               INT          NOT NULL AUTO_INCREMENT,
                    `UserId`           INT          NOT NULL,
                    `AlertId`          INT          NULL,
                    `ArticleId`        INT          NULL,
                    `Title`            VARCHAR(100) NOT NULL DEFAULT '',
                    `Message`          VARCHAR(500) NOT NULL DEFAULT '',
                    `NotificationType` VARCHAR(50)  NOT NULL DEFAULT 'general',
                    `Type`             VARCHAR(20)  NULL,
                    `Link`             VARCHAR(500) NULL,
                    `IsRead`           TINYINT(1)   NOT NULL DEFAULT 0,
                    `CreatedAt`        DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
                    PRIMARY KEY (`Id`),
                    INDEX `IX_Notifications_UserId` (`UserId`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            await AddColumnIfMissingAsync("Link", "VARCHAR(500) NULL");
            await AddColumnIfMissingAsync("Type", "VARCHAR(20) NULL");
            await AddColumnIfMissingAsync("ArticleId", "INT NULL");
            await AddColumnIfMissingAsync("NotificationType", "VARCHAR(50) NOT NULL DEFAULT 'general'");
            _tableChecked = true;
        }

        private async Task AddColumnIfMissingAsync(string columnName, string definition)
        {
            var exists = await _db.Database
                .SqlQueryRaw<int>(
                    @"SELECT COUNT(*) AS `Value`
                      FROM INFORMATION_SCHEMA.COLUMNS
                      WHERE TABLE_SCHEMA = DATABASE()
                        AND TABLE_NAME = 'Notifications'
                        AND COLUMN_NAME = {0}",
                    columnName)
                .SingleAsync();

            if (exists == 0)
            {
                // Note: Column names cannot be parameterized, but these are controlled internally.
                // We use ExecuteSqlRawAsync with a concatenated string to avoid the interpolation warning.
                var sql = "ALTER TABLE `Notifications` ADD COLUMN `" + columnName + "` " + definition + ";";
                await _db.Database.ExecuteSqlRawAsync(sql);
            }
        }

        // Tạo thông báo mới
        public async Task CreateAsync(int userId, string type, string title, string message, string? link = null)
        {
            await EnsureTableAsync();

            // Dùng raw SQL để tránh bất kỳ schema mismatch nào
            await _db.Database.ExecuteSqlAsync($@"
                INSERT INTO `Notifications` (`UserId`, `Title`, `Message`, `NotificationType`, `Type`, `Link`, `IsRead`, `CreatedAt`)
                VALUES ({userId}, {title}, {message}, {type}, {type}, {link}, 0, NOW())");
        }

        // Lấy thông báo chưa đọc của user (tối đa 20)
        public async Task<List<Notification>> GetUnreadAsync(int userId)
        {
            await EnsureTableAsync();
            return await _db.Notifications
                .FromSqlRaw("SELECT * FROM `Notifications` WHERE `UserId` = {0} AND `IsRead` = 0 ORDER BY `CreatedAt` DESC LIMIT 20", userId)
                .ToListAsync();
        }

        // Lấy tất cả thông báo gần đây (đọc + chưa đọc, 30 cái)
        public async Task<List<Notification>> GetRecentAsync(int userId)
        {
            await EnsureTableAsync();
            return await _db.Notifications
                .FromSqlRaw("SELECT * FROM `Notifications` WHERE `UserId` = {0} ORDER BY `CreatedAt` DESC LIMIT 30", userId)
                .ToListAsync();
        }

        // Đánh dấu 1 thông báo đã đọc
        public async Task MarkReadAsync(int id, int userId)
        {
            await EnsureTableAsync();
            await _db.Database.ExecuteSqlAsync(
                $"UPDATE `Notifications` SET `IsRead` = 1 WHERE `Id` = {id} AND `UserId` = {userId}");
        }

        // Đánh dấu tất cả đã đọc
        public async Task MarkAllReadAsync(int userId)
        {
            await EnsureTableAsync();
            await _db.Database.ExecuteSqlAsync(
                $"UPDATE `Notifications` SET `IsRead` = 1 WHERE `UserId` = {userId}");
        }

        // Đếm chưa đọc
        public async Task<int> CountUnreadAsync(int userId)
        {
            await EnsureTableAsync();
            return await _db.Notifications
                .FromSqlRaw("SELECT * FROM `Notifications` WHERE `UserId` = {0} AND `IsRead` = 0", userId)
                .CountAsync();
        }
    }
}
