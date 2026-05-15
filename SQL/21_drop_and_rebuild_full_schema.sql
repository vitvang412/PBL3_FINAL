DROP DATABASE IF EXISTS DaNangSafeMap;

CREATE DATABASE DaNangSafeMap
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE DaNangSafeMap;

CREATE TABLE Users (
    Id              INT PRIMARY KEY AUTO_INCREMENT,
    FullName        VARCHAR(100) CHARACTER SET utf8mb4 NOT NULL,
    DateOfBirth     DATE NULL,
    Gender          ENUM('Nam', 'Nữ', 'Khác') NULL,
    Address         VARCHAR(255) CHARACTER SET utf8mb4 NULL,
    Avatar          VARCHAR(500) NULL,
    Email           VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255) NULL,
    GoogleId        VARCHAR(255) NULL UNIQUE,
    AuthProvider    ENUM('Local', 'Google') NOT NULL DEFAULT 'Local',
    Role            ENUM('Admin', 'User') NOT NULL DEFAULT 'User',
    ReputationScore INT NOT NULL DEFAULT 5,
    IsActive        BOOLEAN NOT NULL DEFAULT TRUE,
    LockedUntil     DATETIME NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastLoginAt     DATETIME NULL,
    INDEX idx_users_role (Role)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE AlertCategories (
    Id          INT PRIMARY KEY AUTO_INCREMENT,
    Name        VARCHAR(100) CHARACTER SET utf8mb4 NOT NULL,
    Slug        VARCHAR(50) NOT NULL UNIQUE,
    Description VARCHAR(500) CHARACTER SET utf8mb4 NULL,
    ColorHex    VARCHAR(7) NOT NULL DEFAULT '#FF6B6B',
    SortOrder   INT NOT NULL DEFAULT 0,
    IsActive    BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE AlertTypes (
    Id          INT PRIMARY KEY AUTO_INCREMENT,
    CategoryId  INT NOT NULL,
    Name        VARCHAR(100) CHARACTER SET utf8mb4 NOT NULL,
    Slug        VARCHAR(50) NOT NULL UNIQUE,
    IconEmoji   VARCHAR(10) NULL,
    IconUrl     VARCHAR(500) NULL,
    SortOrder   INT NOT NULL DEFAULT 0,
    IsActive    BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_AlertTypes_Category FOREIGN KEY (CategoryId) REFERENCES AlertCategories(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE SecurityAlerts (
    Id                   INT PRIMARY KEY AUTO_INCREMENT,
    UserId               INT NOT NULL,
    AlertTypeId          INT NOT NULL,
    Latitude             DECIMAL(10, 8) NOT NULL,
    Longitude            DECIMAL(11, 8) NOT NULL,
    AddressText          VARCHAR(500) CHARACTER SET utf8mb4 NULL,
    Title                VARCHAR(200) CHARACTER SET utf8mb4 NOT NULL,
    Description          TEXT CHARACTER SET utf8mb4 NOT NULL,
    IncidentTime         DATETIME NOT NULL,
    Status               ENUM(
        'PENDING_REVIEW',
        'VISIBLE_UNVERIFIED',
        'VISIBLE_VERIFIED',
        'RESOLVED',
        'REJECTED',
        'EXPIRED',
        'NEEDS_MORE_INFO',
        'NOT_ENOUGH_EVIDENCE'
    ) NOT NULL DEFAULT 'PENDING_REVIEW',
    TrustScore           INT NOT NULL DEFAULT 0,
    RoutingDecision      VARCHAR(20) NOT NULL DEFAULT 'GREEN',
    FilterReason         VARCHAR(500) NULL,
    TrustScoreBreakdown  VARCHAR(1000) NULL,
    ModerationReason     VARCHAR(500) NULL,
    MoreInfoDeadline     DATETIME NULL,
    ReviewPriority       VARCHAR(20) NOT NULL DEFAULT 'NORMAL',
    ReviewDueAt          DATETIME NULL,
    DisplayPriority      INT NOT NULL DEFAULT 50,
    FirstVisibleAt       DATETIME NULL,
    AutoHideAt           DATETIME NULL,
    ConfirmCount         INT NOT NULL DEFAULT 0,
    DenyCount            INT NOT NULL DEFAULT 0,
    Opacity              INT NOT NULL DEFAULT 30,
    HasMedia             BOOLEAN NOT NULL DEFAULT FALSE,
    UserConfirmed        BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedAt            DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt            DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    ResolvedAt           DATETIME NULL,
    ExpiresAt            DATETIME NULL,
    CONSTRAINT FK_SecurityAlerts_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_SecurityAlerts_AlertType FOREIGN KEY (AlertTypeId) REFERENCES AlertTypes(Id),
    INDEX idx_alerts_location (Latitude, Longitude),
    INDEX idx_alerts_status_time (Status, CreatedAt DESC),
    INDEX idx_alerts_incident (IncidentTime DESC),
    INDEX idx_alerts_expires (ExpiresAt),
    INDEX IX_SecurityAlerts_UserId_CreatedAt (UserId, CreatedAt),
    INDEX IX_SecurityAlerts_RoutingDecision_CreatedAt (RoutingDecision, CreatedAt),
    INDEX IX_SecurityAlerts_Status_ReviewPriority_ReviewDueAt (Status, ReviewPriority, ReviewDueAt),
    INDEX IX_SecurityAlerts_Status_DisplayPriority_AutoHideAt (Status, DisplayPriority, AutoHideAt),
    INDEX IX_SecurityAlerts_AutoHideAt (AutoHideAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE AlertMedia (
    Id          INT PRIMARY KEY AUTO_INCREMENT,
    AlertId     INT NOT NULL,
    UserId      INT NOT NULL,
    MediaType   ENUM('IMAGE', 'VIDEO') NOT NULL DEFAULT 'IMAGE',
    FilePath    VARCHAR(500) NOT NULL,
    FileName    VARCHAR(255) NOT NULL,
    FileSize    BIGINT NULL,
    SourceType  ENUM('ORIGINAL', 'VERIFICATION') NOT NULL DEFAULT 'ORIGINAL',
    IsActive    BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_AlertMedia_Alert FOREIGN KEY (AlertId) REFERENCES SecurityAlerts(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AlertMedia_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    INDEX idx_media_alert (AlertId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE AlertVerifications (
    Id                INT PRIMARY KEY AUTO_INCREMENT,
    AlertId           INT NOT NULL,
    UserId            INT NOT NULL,
    VerificationType  ENUM('CONFIRM', 'DENY') NOT NULL,
    Latitude          DECIMAL(10, 8) NULL,
    Longitude         DECIMAL(11, 8) NULL,
    Comment           TEXT CHARACTER SET utf8mb4 NULL,
    PreviousType      ENUM('CONFIRM', 'DENY') NULL,
    ChangedAt         DATETIME NULL,
    CreatedAt         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY UK_Alert_User (AlertId, UserId),
    CONSTRAINT FK_AlertVerifications_Alert FOREIGN KEY (AlertId) REFERENCES SecurityAlerts(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AlertVerifications_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    INDEX idx_verifications (AlertId, VerificationType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE ModerationLogs (
    Id              INT PRIMARY KEY AUTO_INCREMENT,
    AlertId         INT NOT NULL,
    AdminUserId     INT NOT NULL,
    ActionType      VARCHAR(50) NOT NULL,
    PreviousStatus  VARCHAR(30) NULL,
    NewStatus       VARCHAR(30) NULL,
    Reason          VARCHAR(500) NULL,
    Detail          VARCHAR(1000) NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_ModerationLogs_Alert FOREIGN KEY (AlertId) REFERENCES SecurityAlerts(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ModerationLogs_AdminUser FOREIGN KEY (AdminUserId) REFERENCES Users(Id) ON DELETE RESTRICT,
    INDEX IX_ModerationLogs_AlertId_CreatedAt (AlertId, CreatedAt),
    INDEX IX_ModerationLogs_AdminUserId_CreatedAt (AdminUserId, CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE Notifications (
    Id               INT PRIMARY KEY AUTO_INCREMENT,
    UserId           INT NOT NULL,
    AlertId          INT NULL,
    Title            VARCHAR(100) NOT NULL,
    Message          VARCHAR(500) NOT NULL,
    NotificationType VARCHAR(50) NOT NULL,
    IsRead           BIT NOT NULL DEFAULT b'0',
    CreatedAt        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_Notifications_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Notifications_Alert FOREIGN KEY (AlertId) REFERENCES SecurityAlerts(Id) ON DELETE SET NULL,
    INDEX IX_Notifications_UserId_IsRead_CreatedAt (UserId, IsRead, CreatedAt),
    INDEX IX_Notifications_AlertId_CreatedAt (AlertId, CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE AlertAppeals (
    Id                 INT PRIMARY KEY AUTO_INCREMENT,
    AlertId            INT NOT NULL,
    UserId             INT NOT NULL,
    Reason             VARCHAR(500) NOT NULL,
    Status             VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    ReviewedByAdminId  INT NULL,
    ReviewNote         VARCHAR(500) NULL,
    CreatedAt          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ReviewedAt         DATETIME NULL,
    CONSTRAINT FK_AlertAppeals_Alert FOREIGN KEY (AlertId) REFERENCES SecurityAlerts(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AlertAppeals_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE RESTRICT,
    CONSTRAINT FK_AlertAppeals_ReviewedByAdmin FOREIGN KEY (ReviewedByAdminId) REFERENCES Users(Id) ON DELETE RESTRICT,
    INDEX IX_AlertAppeals_AlertId_Status_CreatedAt (AlertId, Status, CreatedAt),
    INDEX IX_AlertAppeals_UserId_CreatedAt (UserId, CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO Users (FullName, Email, PasswordHash, Role, IsActive, AuthProvider, ReputationScore)
VALUES (
    'Nguyễn Thanh Hằng',
    'hang290806@danang.gov.vn',
    '$2a$12$JPDmarWajaDUEfNYbQfEReNrUfmqKUFn6OIK3UsR3jrbJPMjIF6Lu',
    'Admin',
    TRUE,
    'Local',
    5
);

INSERT INTO AlertCategories (Name, Slug, Description, ColorHex, SortOrder, IsActive) VALUES
('Xâm phạm Sở hữu', 'property_crime', 'Trộm cắp, móc túi, cướp giật, đột nhập', '#E74C3C', 1, TRUE),
('Trật tự An toàn Xã hội', 'public_disorder', 'Đua xe, ẩu đả, gây rối trật tự', '#F39C12', 2, TRUE),
('An ninh Du lịch & Lừa đảo', 'tourism_security', 'Lừa đảo, chèo kéo, chặt chém du khách', '#E67E22', 3, TRUE);

INSERT INTO AlertTypes (CategoryId, Name, Slug, IconEmoji, IconUrl, SortOrder, IsActive) VALUES
(1, 'Trộm cắp xe máy', 'theft_motorbike', NULL, NULL, 1, TRUE),
(1, 'Móc túi / Cướp giật', 'pickpocket_robbery', NULL, NULL, 2, TRUE),
(1, 'Trộm đột nhập', 'burglary', NULL, NULL, 3, TRUE),
(2, 'Đua xe / Nẹt pô', 'street_racing', NULL, NULL, 4, TRUE),
(2, 'Ẩu đả / Gây rối', 'fighting_disorder', NULL, NULL, 5, TRUE),
(3, 'Lừa đảo / Chèo kéo', 'scam_tourist', NULL, NULL, 6, TRUE),
(3, 'Chặt chém giá', 'overcharging', NULL, NULL, 7, TRUE);

INSERT INTO Users (Id, FullName, Email, PasswordHash, Role, IsActive, AuthProvider, ReputationScore, Address)
VALUES
(2, 'Trần Minh An', 'user1@danang.local', '$2a$12$JPDmarWajaDUEfNYbQfEReNrUfmqKUFn6OIK3UsR3jrbJPMjIF6Lu', 'User', TRUE, 'Local', 6, 'Hải Châu, Đà Nẵng'),
(3, 'Lê Thu Hà', 'user2@danang.local', '$2a$12$JPDmarWajaDUEfNYbQfEReNrUfmqKUFn6OIK3UsR3jrbJPMjIF6Lu', 'User', TRUE, 'Local', 5, 'Sơn Trà, Đà Nẵng');

INSERT INTO SecurityAlerts (
    Id, UserId, AlertTypeId, Latitude, Longitude, AddressText, Title, Description,
    IncidentTime, Status, TrustScore, RoutingDecision, FilterReason, TrustScoreBreakdown,
    ModerationReason, MoreInfoDeadline, ReviewPriority, ReviewDueAt, DisplayPriority,
    FirstVisibleAt, AutoHideAt, ConfirmCount, DenyCount, Opacity, HasMedia, UserConfirmed,
    CreatedAt, UpdatedAt, ResolvedAt, ExpiresAt
) VALUES
(
    1, 2, 1, 16.06778000, 108.22083100, 'Gần chợ Hàn, Hải Châu, Đà Nẵng',
    'Nghi vấn trộm cắp xe máy',
    'Người dân ghi nhận một nhóm thanh niên có dấu hiệu bẻ khóa xe máy ở khu vực gần chợ Hàn. Báo cáo này có kèm ảnh hiện trường để kiểm tra nhanh trên bản đồ.',
    DATE_SUB(NOW(), INTERVAL 2 HOUR), 'VISIBLE_VERIFIED', 82, 'GREEN', NULL, 'Ảnh rõ +20; mô tả chi tiết +20; tài khoản uy tín +20; xác nhận cộng đồng +22',
    NULL, NULL, 'NORMAL', DATE_ADD(NOW(), INTERVAL 10 HOUR), 95,
    DATE_SUB(NOW(), INTERVAL 110 MINUTE), DATE_ADD(NOW(), INTERVAL 30 DAY), 4, 0, 100, TRUE, TRUE,
    DATE_SUB(NOW(), INTERVAL 110 MINUTE), DATE_SUB(NOW(), INTERVAL 100 MINUTE), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)
),
(
    2, 3, 4, 16.06125000, 108.24320000, 'Đường Võ Nguyên Giáp, Sơn Trà, Đà Nẵng',
    'Tụ tập nẹt pô về đêm',
    'Có nhóm xe máy tụ tập nẹt pô và chạy tốc độ cao vào buổi tối. Báo cáo mẫu này có kèm video để kiểm tra luồng hiển thị media trên popup.',
    DATE_SUB(NOW(), INTERVAL 5 HOUR), 'VISIBLE_UNVERIFIED', 58, 'GREEN', NULL, 'Mô tả chi tiết +20; tài khoản thường +10; chưa đủ xác minh cộng đồng',
    NULL, NULL, 'NORMAL', DATE_ADD(NOW(), INTERVAL 7 HOUR), 70,
    DATE_SUB(NOW(), INTERVAL 4 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 1, 0, 30, TRUE, TRUE,
    DATE_SUB(NOW(), INTERVAL 4 HOUR), DATE_SUB(NOW(), INTERVAL 3 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)
);

INSERT INTO AlertMedia (
    AlertId, UserId, MediaType, FilePath, FileName, FileSize, SourceType, IsActive, CreatedAt
) VALUES
(1, 2, 'IMAGE', '/uploads/alerts/1/d2dfb400-6a90-4899-b254-5a5025766f0e.png', 'd2dfb400-6a90-4899-b254-5a5025766f0e.png', 0, 'ORIGINAL', TRUE, DATE_SUB(NOW(), INTERVAL 105 MINUTE)),
(1, 2, 'IMAGE', '/uploads/alerts/1/5992cc52-661c-410e-8295-b37a36d2144f.jpg', '5992cc52-661c-410e-8295-b37a36d2144f.jpg', 0, 'ORIGINAL', TRUE, DATE_SUB(NOW(), INTERVAL 103 MINUTE)),
(2, 3, 'VIDEO', '/uploads/alerts/2/41d30a93-c83d-4656-9e89-d04eab591767.mp4', '41d30a93-c83d-4656-9e89-d04eab591767.mp4', 0, 'ORIGINAL', TRUE, DATE_SUB(NOW(), INTERVAL 170 MINUTE));

INSERT INTO Users (Id, FullName, Email, PasswordHash, Role, IsActive, AuthProvider, ReputationScore, Address)
VALUES
(4, 'Phạm Quốc Bảo', 'user3@danang.local', '$2a$12$JPDmarWajaDUEfNYbQfEReNrUfmqKUFn6OIK3UsR3jrbJPMjIF6Lu', 'User', TRUE, 'Local', 7, 'Ngũ Hành Sơn, Đà Nẵng'),
(5, 'Nguyễn Mỹ Linh', 'user4@danang.local', '$2a$12$JPDmarWajaDUEfNYbQfEReNrUfmqKUFn6OIK3UsR3jrbJPMjIF6Lu', 'User', TRUE, 'Local', 4, 'Hội An, Quảng Nam'),
(6, 'Võ Gia Huy', 'user5@danang.local', '$2a$12$JPDmarWajaDUEfNYbQfEReNrUfmqKUFn6OIK3UsR3jrbJPMjIF6Lu', 'User', TRUE, 'Local', 8, 'Liên Chiểu, Đà Nẵng');

INSERT INTO SecurityAlerts (
    Id, UserId, AlertTypeId, Latitude, Longitude, AddressText, Title, Description,
    IncidentTime, Status, TrustScore, RoutingDecision, FilterReason, TrustScoreBreakdown,
    ModerationReason, MoreInfoDeadline, ReviewPriority, ReviewDueAt, DisplayPriority,
    FirstVisibleAt, AutoHideAt, ConfirmCount, DenyCount, Opacity, HasMedia, UserConfirmed,
    CreatedAt, UpdatedAt, ResolvedAt, ExpiresAt
) VALUES
(3, 4, 2, 16.05450000, 108.20210000, 'Chợ Cồn, Thanh Khê, Đà Nẵng', 'Nghi móc túi khu vực đông người', 'Có người dân phản ánh xuất hiện đối tượng áp sát khách mua sắm tại khu vực chợ Cồn. Mẫu này dùng để test marker đã xác thực có ảnh.', DATE_SUB(NOW(), INTERVAL 6 HOUR), 'VISIBLE_VERIFIED', 76, 'GREEN', NULL, 'Ảnh rõ +20; mô tả tốt +20; uy tín user +16; xác nhận cộng đồng +20', NULL, NULL, 'NORMAL', NULL, 88, DATE_SUB(NOW(), INTERVAL 5 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 3, 0, 100, TRUE, TRUE, DATE_SUB(NOW(), INTERVAL 5 HOUR), DATE_SUB(NOW(), INTERVAL 4 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)),
(4, 2, 5, 16.07120000, 108.22450000, 'Cầu Rồng, Hải Châu, Đà Nẵng', 'Xô xát nhỏ gần cầu Rồng', 'Có nhóm thanh niên cãi vã lớn tiếng và xô đẩy nhau gần đầu cầu Rồng. Báo cáo này giữ trạng thái chưa xác thực để test độ mờ marker.', DATE_SUB(NOW(), INTERVAL 12 HOUR), 'VISIBLE_UNVERIFIED', 44, 'GREEN', NULL, 'Mô tả ổn +14; chưa có media; chưa đủ xác minh cộng đồng', NULL, NULL, 'NORMAL', NULL, 60, DATE_SUB(NOW(), INTERVAL 11 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 1, 0, 30, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 11 HOUR), DATE_SUB(NOW(), INTERVAL 10 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)),
(5, 5, 6, 15.88010000, 108.33880000, 'Phố cổ Hội An, Quảng Nam', 'Du khách phản ánh chèo kéo', 'Một nhóm du khách phản ánh bị bám theo mời mua hàng và ép sử dụng dịch vụ tại khu phố cổ. Có ảnh minh họa để test popup media.', DATE_SUB(NOW(), INTERVAL 18 HOUR), 'VISIBLE_VERIFIED', 80, 'GREEN', NULL, 'Ảnh rõ +20; mô tả tốt +20; xác nhận cộng đồng +24', NULL, NULL, 'NORMAL', NULL, 90, DATE_SUB(NOW(), INTERVAL 17 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 4, 0, 100, TRUE, TRUE, DATE_SUB(NOW(), INTERVAL 17 HOUR), DATE_SUB(NOW(), INTERVAL 16 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)),
(6, 3, 4, 16.03040000, 108.21870000, 'Đường 2/9, Hải Châu, Đà Nẵng', 'Nhóm xe tụ tập nẹt pô đã giải tán', 'Báo cáo mẫu về đua xe và nẹt pô đã được người đăng đánh dấu là đã xử lý xong. Có video để test popup video và trạng thái resolved.', DATE_SUB(NOW(), INTERVAL 22 HOUR), 'RESOLVED', 63, 'GREEN', NULL, 'Video rõ +20; mô tả tốt +18; xác nhận cộng đồng +10', NULL, NULL, 'NORMAL', NULL, 82, DATE_SUB(NOW(), INTERVAL 21 HOUR), DATE_ADD(NOW(), INTERVAL 14 DAY), 2, 0, 100, TRUE, TRUE, DATE_SUB(NOW(), INTERVAL 21 HOUR), DATE_SUB(NOW(), INTERVAL 18 HOUR), DATE_SUB(NOW(), INTERVAL 17 HOUR), DATE_ADD(NOW(), INTERVAL 14 DAY)),
(7, 6, 7, 15.87760000, 108.32790000, 'Chợ đêm Hội An, Quảng Nam', 'Khách phản ánh chặt chém đồ uống', 'Một quầy bán nước bị phản ánh báo giá cao bất thường cho khách du lịch. Tin này dùng để test thang thời gian 48 giờ.', DATE_SUB(NOW(), INTERVAL 28 HOUR), 'VISIBLE_VERIFIED', 71, 'GREEN', NULL, 'Mô tả tốt +18; user uy tín +18; xác nhận cộng đồng +18', NULL, NULL, 'NORMAL', NULL, 78, DATE_SUB(NOW(), INTERVAL 27 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 2, 0, 100, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 27 HOUR), DATE_SUB(NOW(), INTERVAL 26 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)),
(8, 4, 3, 16.07390000, 108.15040000, 'Khu dân cư Liên Chiểu, Đà Nẵng', 'Cửa hàng bị nghi có dấu hiệu đột nhập', 'Người dân báo cửa cuốn cửa hàng có dấu hiệu bị cạy vào ban đêm. Tin này có ảnh và đang ở trạng thái chưa xác thực.', DATE_SUB(NOW(), INTERVAL 34 HOUR), 'VISIBLE_UNVERIFIED', 52, 'GREEN', NULL, 'Ảnh rõ +20; mô tả khá +16; chưa đủ xác minh cộng đồng', NULL, NULL, 'NORMAL', NULL, 66, DATE_SUB(NOW(), INTERVAL 33 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 1, 0, 30, TRUE, TRUE, DATE_SUB(NOW(), INTERVAL 33 HOUR), DATE_SUB(NOW(), INTERVAL 32 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)),
(9, 2, 1, 16.10580000, 108.17620000, 'Ký túc xá phía Tây, Đà Nẵng', 'Mất xe máy trong bãi giữ xe', 'Sinh viên phản ánh mất xe máy trong khu vực bãi giữ xe tập trung. Dữ liệu này dùng để tăng mật độ heatmap khu phía tây.', DATE_SUB(NOW(), INTERVAL 42 HOUR), 'VISIBLE_VERIFIED', 79, 'GREEN', NULL, 'Ảnh rõ +20; mô tả tốt +20; xác nhận cộng đồng +20', NULL, NULL, 'HOT', NULL, 92, DATE_SUB(NOW(), INTERVAL 41 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 3, 0, 100, TRUE, TRUE, DATE_SUB(NOW(), INTERVAL 41 HOUR), DATE_SUB(NOW(), INTERVAL 40 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)),
(10, 5, 2, 15.97530000, 108.26510000, 'Điện Bàn, Quảng Nam', 'Khách bị giật túi khi dừng đèn đỏ', 'Người đi đường phản ánh bị áp sát giật túi xách tại nút giao đông phương tiện. Đây là tin cũ hơn 2 ngày để test slider 7 ngày.', DATE_SUB(NOW(), INTERVAL 3 DAY), 'VISIBLE_VERIFIED', 68, 'GREEN', NULL, 'Mô tả tốt +18; xác nhận cộng đồng +18', NULL, NULL, 'NORMAL', NULL, 75, DATE_SUB(NOW(), INTERVAL 70 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 2, 0, 100, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 70 HOUR), DATE_SUB(NOW(), INTERVAL 68 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)),
(11, 3, 1, 16.05890000, 108.21040000, 'Ngã tư Lê Duẩn, Hải Châu, Đà Nẵng', 'Cần bổ sung ảnh biển số xe nghi vấn', 'Admin đã yêu cầu người báo bổ sung thêm ảnh biển số để xác thực chính xác phương tiện. Tin này dùng để test toggle hiện tin ẩn trên bản đồ.', DATE_SUB(NOW(), INTERVAL 5 HOUR), 'NEEDS_MORE_INFO', 40, 'YELLOW', NULL, 'Thiếu bằng chứng bổ sung', 'Cần bổ sung ảnh biển số hoặc góc chụp rõ hơn', DATE_ADD(NOW(), INTERVAL 18 HOUR), 'HOT', NULL, 58, DATE_SUB(NOW(), INTERVAL 4 HOUR), DATE_ADD(NOW(), INTERVAL 18 HOUR), 0, 0, 30, TRUE, TRUE, DATE_SUB(NOW(), INTERVAL 4 HOUR), DATE_SUB(NOW(), INTERVAL 2 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 18 HOUR)),
(12, 6, 6, 15.89240000, 108.32060000, 'Bãi biển An Bàng, Hội An', 'Báo cáo chèo kéo không khớp hiện trường', 'Báo cáo này được admin bác bỏ vì mô tả không khớp với dữ liệu hiện trường và phản hồi từ khu vực liên quan.', DATE_SUB(NOW(), INTERVAL 4 DAY), 'REJECTED', 22, 'RED', 'Nội dung không khớp', 'Điểm tin cậy thấp; không đủ căn cứ', 'Bác bỏ do nội dung không khớp hiện trường', NULL, 'NORMAL', NULL, 20, NULL, DATE_SUB(NOW(), INTERVAL 3 DAY), 0, 1, 30, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 4 DAY), DATE_SUB(NOW(), INTERVAL 3 DAY), NULL, DATE_SUB(NOW(), INTERVAL 3 DAY)),
(13, 2, 7, 15.57350000, 108.47490000, 'Tam Kỳ, Quảng Nam', 'Điểm bán hàng từng bị phản ánh đã hết hạn hiển thị', 'Báo cáo cũ về chặt chém đã quá hạn vòng đời và chuyển sang expired để test chế độ xem tin ẩn.', DATE_SUB(NOW(), INTERVAL 8 DAY), 'EXPIRED', 35, 'GREEN', NULL, 'Tin cũ quá vòng đời hiển thị', 'Tự động ẩn do hết hạn hiển thị', NULL, 'NORMAL', NULL, 15, DATE_SUB(NOW(), INTERVAL 7 DAY), DATE_SUB(NOW(), INTERVAL 2 DAY), 1, 0, 30, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 8 DAY), DATE_SUB(NOW(), INTERVAL 2 DAY), NULL, DATE_SUB(NOW(), INTERVAL 2 DAY)),
(14, 4, 3, 15.95020000, 108.25230000, 'Ngũ Hành Sơn, Đà Nẵng', 'Thông tin đột nhập chưa đủ cơ sở', 'Báo cáo có mô tả ngắn và không có góc chụp rõ, nên đang ở trạng thái không đủ cơ sở để hiển thị công khai.', DATE_SUB(NOW(), INTERVAL 2 DAY), 'NOT_ENOUGH_EVIDENCE', 28, 'YELLOW', NULL, 'Thiếu ảnh rõ; mô tả ngắn', 'Không đủ cơ sở để hiển thị công khai', NULL, 'NORMAL', NULL, 24, NULL, DATE_ADD(NOW(), INTERVAL 20 DAY), 0, 0, 30, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 47 HOUR), DATE_SUB(NOW(), INTERVAL 46 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 20 DAY)),
(15, 5, 5, 16.04740000, 108.18880000, 'Công viên 29/3, Đà Nẵng', 'Xô xát tại công viên đã được xử lý', 'Có mâu thuẫn giữa hai nhóm nhỏ tại công viên và đã được lực lượng chức năng địa phương can thiệp. Tin này có ảnh để test nhóm resolved.', DATE_SUB(NOW(), INTERVAL 6 DAY), 'RESOLVED', 61, 'GREEN', NULL, 'Ảnh hiện trường +20; mô tả tốt +16', NULL, NULL, 'NORMAL', NULL, 73, DATE_SUB(NOW(), INTERVAL 143 HOUR), DATE_ADD(NOW(), INTERVAL 14 DAY), 2, 0, 100, TRUE, TRUE, DATE_SUB(NOW(), INTERVAL 143 HOUR), DATE_SUB(NOW(), INTERVAL 140 HOUR), DATE_SUB(NOW(), INTERVAL 138 HOUR), DATE_ADD(NOW(), INTERVAL 14 DAY)),
(16, 6, 4, 15.77980000, 108.10050000, 'Điện Bàn, Quảng Nam', 'Nhóm xe tụ tập tăng ga kéo dài', 'Nhiều người dân báo tiếng xe nẹt pô lặp lại vào khung giờ tối muộn. Dữ liệu này dùng để dàn đều theo khu vực Quảng Nam.', DATE_SUB(NOW(), INTERVAL 9 DAY), 'VISIBLE_VERIFIED', 66, 'GREEN', NULL, 'Mô tả tốt +16; xác nhận cộng đồng +20', NULL, NULL, 'HOT', NULL, 80, DATE_SUB(NOW(), INTERVAL 215 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 3, 0, 100, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 215 HOUR), DATE_SUB(NOW(), INTERVAL 214 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)),
(17, 3, 1, 16.09020000, 108.13870000, 'Liên Chiểu, Đà Nẵng', 'Mất xe trước cổng nhà trọ', 'Người thuê trọ phát hiện xe máy biến mất vào sáng sớm. Tin này giữ trạng thái chưa xác thực để test cụm marker cũ hơn 1 tuần.', DATE_SUB(NOW(), INTERVAL 11 DAY), 'VISIBLE_UNVERIFIED', 38, 'GREEN', NULL, 'Mô tả trung bình; chưa có media', NULL, NULL, 'NORMAL', NULL, 52, DATE_SUB(NOW(), INTERVAL 263 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 0, 0, 30, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 263 HOUR), DATE_SUB(NOW(), INTERVAL 260 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)),
(18, 2, 6, 15.87920000, 108.33550000, 'Chùa Cầu, Hội An', 'Khách nước ngoài bị mời mua tour giá cao', 'Một nhóm khách phản ánh bị chèo kéo mua tour và dịch vụ với giá cao hơn niêm yết. Có video để test popup ở khu phố cổ.', DATE_SUB(NOW(), INTERVAL 13 DAY), 'VISIBLE_VERIFIED', 74, 'GREEN', NULL, 'Video rõ +20; mô tả tốt +18; xác nhận cộng đồng +18', NULL, NULL, 'NORMAL', NULL, 84, DATE_SUB(NOW(), INTERVAL 311 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 2, 0, 100, TRUE, TRUE, DATE_SUB(NOW(), INTERVAL 311 HOUR), DATE_SUB(NOW(), INTERVAL 308 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)),
(19, 4, 7, 15.98110000, 108.25570000, 'Ngũ Hành Sơn, Đà Nẵng', 'Phản ánh giá gửi xe cao bất thường', 'Du khách phản ánh bị thu phí gửi xe cao hơn mức niêm yết tại một điểm gần bãi biển. Tin này dùng để tăng dữ liệu nhóm du lịch.', DATE_SUB(NOW(), INTERVAL 15 DAY), 'VISIBLE_UNVERIFIED', 41, 'GREEN', NULL, 'Mô tả khá; chưa có ảnh xác minh', NULL, NULL, 'NORMAL', NULL, 57, DATE_SUB(NOW(), INTERVAL 359 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 1, 0, 30, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 359 HOUR), DATE_SUB(NOW(), INTERVAL 357 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)),
(20, 5, 3, 16.06710000, 108.19090000, 'Thanh Khê, Đà Nẵng', 'Nhà dân nghi bị đột nhập lúc rạng sáng', 'Chủ nhà phát hiện cửa sau có dấu hiệu cạy mở và mất một số tài sản nhỏ. Tin này giúp tăng dữ liệu cụm dân cư trung tâm.', DATE_SUB(NOW(), INTERVAL 17 DAY), 'VISIBLE_VERIFIED', 69, 'GREEN', NULL, 'Mô tả tốt +18; xác nhận cộng đồng +18', NULL, NULL, 'NORMAL', NULL, 79, DATE_SUB(NOW(), INTERVAL 407 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 2, 0, 100, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 407 HOUR), DATE_SUB(NOW(), INTERVAL 405 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)),
(21, 6, 5, 16.05270000, 108.24380000, 'Bãi biển Mỹ Khê, Đà Nẵng', 'Hai nhóm khách cãi vã lớn tiếng', 'Có xô xát lời nói kéo dài tại khu vực bãi biển đông khách vào buổi tối. Dữ liệu này giúp heatmap có thêm điểm ở ven biển.', DATE_SUB(NOW(), INTERVAL 20 DAY), 'VISIBLE_VERIFIED', 64, 'GREEN', NULL, 'Mô tả tốt +18; xác nhận cộng đồng +16', NULL, NULL, 'NORMAL', NULL, 74, DATE_SUB(NOW(), INTERVAL 479 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 2, 0, 100, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 479 HOUR), DATE_SUB(NOW(), INTERVAL 476 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)),
(22, 2, 4, 16.11440000, 108.13260000, 'Hòa Khánh, Đà Nẵng', 'Nhiều xe máy tụ tập gần khu công nghiệp', 'Người dân phản ánh nhiều xe máy tụ tập và rú ga liên tục vào khung giờ khuya gần khu công nghiệp.', DATE_SUB(NOW(), INTERVAL 22 DAY), 'VISIBLE_UNVERIFIED', 36, 'GREEN', NULL, 'Mô tả ổn; chưa có media', NULL, NULL, 'NORMAL', NULL, 50, DATE_SUB(NOW(), INTERVAL 527 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 0, 0, 30, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 527 HOUR), DATE_SUB(NOW(), INTERVAL 525 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)),
(23, 3, 1, 15.93680000, 108.28640000, 'Điện Nam, Quảng Nam', 'Mất xe máy trước quán ăn đêm', 'Khách để xe trước quán ăn đêm và quay ra thì không còn xe. Tin này có ảnh để test nhóm dữ liệu gần mốc 30 ngày.', DATE_SUB(NOW(), INTERVAL 24 DAY), 'VISIBLE_VERIFIED', 72, 'GREEN', NULL, 'Ảnh rõ +20; mô tả tốt +18; xác nhận cộng đồng +18', NULL, NULL, 'NORMAL', NULL, 83, DATE_SUB(NOW(), INTERVAL 575 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 2, 0, 100, TRUE, TRUE, DATE_SUB(NOW(), INTERVAL 575 HOUR), DATE_SUB(NOW(), INTERVAL 573 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)),
(24, 4, 2, 16.07550000, 108.22190000, 'Trung tâm Hải Châu, Đà Nẵng', 'Mất điện thoại và đã tìm lại được', 'Người báo trước đó nghi bị móc túi nhưng sau đó đã tìm lại được tài sản nên chuyển trạng thái resolved.', DATE_SUB(NOW(), INTERVAL 26 DAY), 'RESOLVED', 54, 'GREEN', NULL, 'Tin đã được cập nhật hoàn tất', NULL, NULL, 'NORMAL', NULL, 62, DATE_SUB(NOW(), INTERVAL 623 HOUR), DATE_ADD(NOW(), INTERVAL 14 DAY), 1, 0, 100, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 623 HOUR), DATE_SUB(NOW(), INTERVAL 620 HOUR), DATE_SUB(NOW(), INTERVAL 618 HOUR), DATE_ADD(NOW(), INTERVAL 14 DAY)),
(25, 5, 6, 15.88660000, 108.34120000, 'Cửa Đại, Quảng Nam', 'Nhóm khách bị mời gọi dịch vụ quá mức', 'Phản ánh về tình trạng mời chào dai dẳng gây khó chịu cho du khách tại khu vực ven biển.', DATE_SUB(NOW(), INTERVAL 29 DAY), 'VISIBLE_VERIFIED', 67, 'GREEN', NULL, 'Mô tả tốt +18; xác nhận cộng đồng +16', NULL, NULL, 'NORMAL', NULL, 76, DATE_SUB(NOW(), INTERVAL 695 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 2, 0, 100, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 695 HOUR), DATE_SUB(NOW(), INTERVAL 692 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)),
(26, 6, 7, 16.06240000, 108.17510000, 'Thanh Khê Tây, Đà Nẵng', 'Giá sửa xe bị phản ánh cao', 'Người dân phản ánh bị báo giá sửa xe cao bất thường. Tin này nằm ngoài 30 ngày để test mốc Tất cả.', DATE_SUB(NOW(), INTERVAL 31 DAY), 'VISIBLE_UNVERIFIED', 33, 'GREEN', NULL, 'Tin cũ ngoài mốc 30 ngày', NULL, NULL, 'NORMAL', NULL, 45, DATE_SUB(NOW(), INTERVAL 743 HOUR), DATE_ADD(NOW(), INTERVAL 20 DAY), 0, 0, 30, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 743 HOUR), DATE_SUB(NOW(), INTERVAL 740 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 20 DAY)),
(27, 2, 3, 15.58820000, 108.48050000, 'Tam Kỳ, Quảng Nam', 'Nhà kho bị nghi cạy cửa', 'Báo cáo cũ hơn một tháng về việc cạy cửa nhà kho để test mốc thời gian dài và heatmap toàn bộ.', DATE_SUB(NOW(), INTERVAL 35 DAY), 'VISIBLE_VERIFIED', 62, 'GREEN', NULL, 'Tin cũ nhưng đã xác minh', NULL, NULL, 'NORMAL', NULL, 72, DATE_SUB(NOW(), INTERVAL 839 HOUR), DATE_ADD(NOW(), INTERVAL 15 DAY), 2, 0, 100, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 839 HOUR), DATE_SUB(NOW(), INTERVAL 836 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 15 DAY)),
(28, 4, 5, 15.97070000, 108.26100000, 'Ngũ Hành Sơn, Đà Nẵng', 'Cần bổ sung clip rõ hơn về vụ gây rối', 'Báo cáo gây rối trật tự đã được admin yêu cầu bổ sung clip rõ hơn trước khi hiển thị công khai.', DATE_SUB(NOW(), INTERVAL 16 HOUR), 'NEEDS_MORE_INFO', 39, 'YELLOW', NULL, 'Thiếu clip rõ mặt đối tượng', 'Cần bổ sung clip rõ hơn để tiếp tục xác thực', DATE_ADD(NOW(), INTERVAL 6 HOUR), 'HOT', NULL, 56, DATE_SUB(NOW(), INTERVAL 15 HOUR), DATE_ADD(NOW(), INTERVAL 6 HOUR), 0, 0, 30, TRUE, TRUE, DATE_SUB(NOW(), INTERVAL 15 HOUR), DATE_SUB(NOW(), INTERVAL 14 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 6 HOUR)),
(29, 5, 6, 16.06880000, 108.23270000, 'Sơn Trà, Đà Nẵng', 'Báo cáo chèo kéo đã bị bác bỏ', 'Tin này được dùng để test lọc trạng thái ẩn do bị reject trong khoảng thời gian dưới 24 giờ.', DATE_SUB(NOW(), INTERVAL 14 HOUR), 'REJECTED', 25, 'RED', 'Không đủ bằng chứng', 'Thiếu media và xác minh', 'Bác bỏ do không đủ bằng chứng kiểm chứng', NULL, 'NORMAL', NULL, 18, NULL, DATE_SUB(NOW(), INTERVAL 10 HOUR), 0, 0, 30, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 13 HOUR), DATE_SUB(NOW(), INTERVAL 10 HOUR), NULL, DATE_SUB(NOW(), INTERVAL 10 HOUR)),
(30, 6, 4, 16.03820000, 108.21250000, 'Cẩm Lệ, Đà Nẵng', 'Báo cáo đua xe đã hết hạn xử lý', 'Báo cáo cũ về tụ tập đua xe đã hết vòng đời và chuyển sang expired để test hidden toggle trong khoảng 48 giờ.', DATE_SUB(NOW(), INTERVAL 36 HOUR), 'EXPIRED', 31, 'GREEN', NULL, 'Tin cũ đã hết vòng đời', 'Tự động ẩn do quá hạn hiển thị', NULL, 'NORMAL', NULL, 16, DATE_SUB(NOW(), INTERVAL 35 HOUR), DATE_SUB(NOW(), INTERVAL 8 HOUR), 0, 0, 30, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 35 HOUR), DATE_SUB(NOW(), INTERVAL 8 HOUR), NULL, DATE_SUB(NOW(), INTERVAL 8 HOUR)),
(31, 2, 7, 15.89290000, 108.34810000, 'Ven sông Hoài, Hội An', 'Khách phản ánh giá dịch vụ cao mùa lễ', 'Dữ liệu mẫu để tăng mật độ nhóm du lịch trong mốc 7 ngày và 30 ngày.', DATE_SUB(NOW(), INTERVAL 55 HOUR), 'VISIBLE_VERIFIED', 70, 'GREEN', NULL, 'Mô tả tốt +18; xác nhận cộng đồng +18', NULL, NULL, 'NORMAL', NULL, 77, DATE_SUB(NOW(), INTERVAL 54 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 2, 0, 100, FALSE, TRUE, DATE_SUB(NOW(), INTERVAL 54 HOUR), DATE_SUB(NOW(), INTERVAL 53 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY)),
(32, 3, 2, 16.08860000, 108.24930000, 'Bán đảo Sơn Trà, Đà Nẵng', 'Du khách báo mất ví khi tham quan', 'Một khách du lịch báo mất ví trong lúc dừng chân tham quan. Báo cáo này có ảnh để test popup ở vùng ven bán đảo.', DATE_SUB(NOW(), INTERVAL 72 HOUR), 'VISIBLE_VERIFIED', 73, 'GREEN', NULL, 'Ảnh rõ +20; mô tả tốt +18; xác nhận cộng đồng +18', NULL, NULL, 'NORMAL', NULL, 81, DATE_SUB(NOW(), INTERVAL 71 HOUR), DATE_ADD(NOW(), INTERVAL 30 DAY), 2, 0, 100, TRUE, TRUE, DATE_SUB(NOW(), INTERVAL 71 HOUR), DATE_SUB(NOW(), INTERVAL 69 HOUR), NULL, DATE_ADD(NOW(), INTERVAL 30 DAY));

INSERT INTO AlertMedia (
    AlertId, UserId, MediaType, FilePath, FileName, FileSize, SourceType, IsActive, CreatedAt
) VALUES
(3, 4, 'IMAGE', '/uploads/alerts/1/d2dfb400-6a90-4899-b254-5a5025766f0e.png', 'd2dfb400-6a90-4899-b254-5a5025766f0e.png', 0, 'ORIGINAL', TRUE, DATE_SUB(NOW(), INTERVAL 290 MINUTE)),
(5, 5, 'IMAGE', '/uploads/alerts/1/5992cc52-661c-410e-8295-b37a36d2144f.jpg', '5992cc52-661c-410e-8295-b37a36d2144f.jpg', 0, 'ORIGINAL', TRUE, DATE_SUB(NOW(), INTERVAL 1010 MINUTE)),
(6, 3, 'VIDEO', '/uploads/alerts/2/41d30a93-c83d-4656-9e89-d04eab591767.mp4', '41d30a93-c83d-4656-9e89-d04eab591767.mp4', 0, 'ORIGINAL', TRUE, DATE_SUB(NOW(), INTERVAL 1200 MINUTE)),
(8, 4, 'IMAGE', '/uploads/alerts/1/d2dfb400-6a90-4899-b254-5a5025766f0e.png', 'd2dfb400-6a90-4899-b254-5a5025766f0e.png', 0, 'ORIGINAL', TRUE, DATE_SUB(NOW(), INTERVAL 1980 MINUTE)),
(9, 2, 'IMAGE', '/uploads/alerts/1/5992cc52-661c-410e-8295-b37a36d2144f.jpg', '5992cc52-661c-410e-8295-b37a36d2144f.jpg', 0, 'ORIGINAL', TRUE, DATE_SUB(NOW(), INTERVAL 2460 MINUTE)),
(11, 3, 'IMAGE', '/uploads/alerts/1/d2dfb400-6a90-4899-b254-5a5025766f0e.png', 'd2dfb400-6a90-4899-b254-5a5025766f0e.png', 0, 'ORIGINAL', TRUE, DATE_SUB(NOW(), INTERVAL 220 MINUTE)),
(15, 5, 'IMAGE', '/uploads/alerts/1/5992cc52-661c-410e-8295-b37a36d2144f.jpg', '5992cc52-661c-410e-8295-b37a36d2144f.jpg', 0, 'ORIGINAL', TRUE, DATE_SUB(NOW(), INTERVAL 8600 MINUTE)),
(18, 2, 'VIDEO', '/uploads/alerts/2/41d30a93-c83d-4656-9e89-d04eab591767.mp4', '41d30a93-c83d-4656-9e89-d04eab591767.mp4', 0, 'ORIGINAL', TRUE, DATE_SUB(NOW(), INTERVAL 18600 MINUTE)),
(23, 3, 'IMAGE', '/uploads/alerts/1/d2dfb400-6a90-4899-b254-5a5025766f0e.png', 'd2dfb400-6a90-4899-b254-5a5025766f0e.png', 0, 'ORIGINAL', TRUE, DATE_SUB(NOW(), INTERVAL 34400 MINUTE)),
(28, 4, 'VIDEO', '/uploads/alerts/2/41d30a93-c83d-4656-9e89-d04eab591767.mp4', '41d30a93-c83d-4656-9e89-d04eab591767.mp4', 0, 'ORIGINAL', TRUE, DATE_SUB(NOW(), INTERVAL 840 MINUTE)),
(32, 3, 'IMAGE', '/uploads/alerts/1/5992cc52-661c-410e-8295-b37a36d2144f.jpg', '5992cc52-661c-410e-8295-b37a36d2144f.jpg', 0, 'ORIGINAL', TRUE, DATE_SUB(NOW(), INTERVAL 4200 MINUTE));
