-- Migration: AddAlertReports
-- Feature 1: Bao cao vi pham trong popup va moderation admin
-- Tuong thich voi MySQL khong ho tro ADD COLUMN IF NOT EXISTS

USE `DaNangSafeMap`;
SELECT DATABASE() AS CurrentDatabase;

SET @is_banned_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Users'
      AND COLUMN_NAME = 'IsBanned'
);

SET @add_is_banned_sql := IF(
    @is_banned_exists = 0,
    'ALTER TABLE `Users` ADD COLUMN `IsBanned` tinyint(1) NOT NULL DEFAULT 0 AFTER `IsActive`',
    'SELECT ''Users.IsBanned already exists'''
);

PREPARE stmt_add_is_banned FROM @add_is_banned_sql;
EXECUTE stmt_add_is_banned;
DEALLOCATE PREPARE stmt_add_is_banned;

CREATE TABLE IF NOT EXISTS `AlertReports` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `AlertId` int NOT NULL,
    `ReporterId` int NOT NULL,
    `Reason` varchar(50) NOT NULL,
    `Description` varchar(300) NULL,
    `Status` varchar(20) NOT NULL DEFAULT 'PENDING',
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_AlertReports_AlertId_ReporterId` (`AlertId`, `ReporterId`),
    KEY `IX_AlertReports_Status_CreatedAt` (`Status`, `CreatedAt`),
    CONSTRAINT `FK_AlertReports_SecurityAlerts_AlertId` FOREIGN KEY (`AlertId`) REFERENCES `SecurityAlerts` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_AlertReports_Users_ReporterId` FOREIGN KEY (`ReporterId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Users'
  AND COLUMN_NAME = 'IsBanned';

SHOW TABLES LIKE 'AlertReports';
