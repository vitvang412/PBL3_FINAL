-- Migration: AddAlertComments
-- Feature 4: Comment system cho bao cao su co
-- Chay tren MySQL 8.0

CREATE TABLE IF NOT EXISTS `AlertComments` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `AlertId` int NOT NULL,
    `UserId` int NOT NULL,
    `Content` varchar(500) NOT NULL,
    `MediaUrl` varchar(255) NULL,
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    INDEX `IX_AlertComments_AlertId_CreatedAt` (`AlertId`, `CreatedAt`),
    CONSTRAINT `FK_AlertComments_SecurityAlerts_AlertId` FOREIGN KEY (`AlertId`) REFERENCES `SecurityAlerts` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_AlertComments_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Neu bang da ton tai, hay chay lenh nay:
-- 
