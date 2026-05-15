using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaNangSafeMap.Migrations
{
    /// <inheritdoc />
    public partial class InitialArticleFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AlertCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ColorHex = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertCategories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FullName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Address = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Avatar = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GoogleId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AuthProvider = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Local")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "User")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    IsBanned = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    LastLoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReputationScore = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    LockedUntil = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AlertTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IconEmoji = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IconUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertTypes_AlertCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "AlertCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "articles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Summary = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImageUrl = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CategoryId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    AuthorId = table.Column<int>(type: "int", nullable: false),
                    ModeratedBy = table.Column<int>(type: "int", nullable: true),
                    ModeratedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    Status = table.Column<int>(type: "int", nullable: true, defaultValue: 1),
                    IsFeatured = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValue: false),
                    IsFocus = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    IsEvent = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    ViewCount = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    RejectReason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    VideoUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AudioUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubCategoryName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_articles_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_articles_Users_ModeratedBy",
                        column: x => x.ModeratedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_articles_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SecurityAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AlertTypeId = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", nullable: false),
                    AddressText = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IncidentTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "PENDING_REVIEW")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrustScore = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RoutingDecision = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "GREEN")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FilterReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrustScoreBreakdown = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewPriority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "NORMAL")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewDueAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DisplayPriority = table.Column<int>(type: "int", nullable: false, defaultValue: 50),
                    FirstVisibleAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AutoHideAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ConfirmCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    DenyCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Opacity = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    HasMedia = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    UserConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    ResolvedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ModerationReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MoreInfoDeadline = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityAlerts_AlertTypes_AlertTypeId",
                        column: x => x.AlertTypeId,
                        principalTable: "AlertTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityAlerts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "article_views",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ViewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_views", x => x.Id);
                    table.ForeignKey(
                        name: "FK_article_views_articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "articlecomments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articlecomments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_articlecomments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_articlecomments_articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AlertAppeals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AlertId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewedByAdminId = table.Column<int>(type: "int", nullable: true),
                    ReviewNote = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    ReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertAppeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertAppeals_SecurityAlerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "SecurityAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlertAppeals_Users_ReviewedByAdminId",
                        column: x => x.ReviewedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AlertAppeals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AlertComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AlertId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MediaUrl = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertComments_SecurityAlerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "SecurityAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlertComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AlertMedia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AlertId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MediaType = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, defaultValue: "IMAGE")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FilePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    SourceType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "ORIGINAL")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertMedia_SecurityAlerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "SecurityAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlertMedia_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AlertReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AlertId = table.Column<int>(type: "int", nullable: false),
                    ReporterId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertReports_SecurityAlerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "SecurityAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlertReports_Users_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AlertVerifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AlertId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    VerificationType = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", nullable: true),
                    Comment = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreviousType = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertVerifications_SecurityAlerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "SecurityAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlertVerifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ModerationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AlertId = table.Column<int>(type: "int", nullable: false),
                    AdminUserId = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreviousStatus = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewStatus = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Detail = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModerationLogs_SecurityAlerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "SecurityAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModerationLogs_Users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AlertId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NotificationType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "SYSTEM")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    ArticleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_SecurityAlerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "SecurityAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AlertAppeals_AlertId_Status_CreatedAt",
                table: "AlertAppeals",
                columns: new[] { "AlertId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertAppeals_ReviewedByAdminId",
                table: "AlertAppeals",
                column: "ReviewedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertAppeals_UserId",
                table: "AlertAppeals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertCategories_Slug",
                table: "AlertCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlertComments_AlertId_CreatedAt",
                table: "AlertComments",
                columns: new[] { "AlertId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertComments_UserId",
                table: "AlertComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertMedia_AlertId",
                table: "AlertMedia",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertMedia_UserId",
                table: "AlertMedia",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertReports_AlertId_ReporterId",
                table: "AlertReports",
                columns: new[] { "AlertId", "ReporterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlertReports_ReporterId",
                table: "AlertReports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertReports_Status_CreatedAt",
                table: "AlertReports",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertTypes_CategoryId",
                table: "AlertTypes",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertTypes_Slug",
                table: "AlertTypes",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlertVerifications_AlertId_UserId",
                table: "AlertVerifications",
                columns: new[] { "AlertId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlertVerifications_AlertId_VerificationType",
                table: "AlertVerifications",
                columns: new[] { "AlertId", "VerificationType" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertVerifications_UserId",
                table: "AlertVerifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_article_views_ArticleId",
                table: "article_views",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_articlecomments_ArticleId",
                table: "articlecomments",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_articlecomments_UserId",
                table: "articlecomments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_articles_AuthorId",
                table: "articles",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_articles_CategoryId",
                table: "articles",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_articles_ModeratedBy",
                table: "articles",
                column: "ModeratedBy");

            migrationBuilder.CreateIndex(
                name: "IX_articles_Slug",
                table: "articles",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_articles_Status",
                table: "articles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_categories_Slug",
                table: "categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModerationLogs_AdminUserId",
                table: "ModerationLogs",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationLogs_AlertId_CreatedAt",
                table: "ModerationLogs",
                columns: new[] { "AlertId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_AlertId",
                table: "Notifications",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ArticleId",
                table: "Notifications",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_AlertTypeId",
                table: "SecurityAlerts",
                column: "AlertTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_AutoHideAt",
                table: "SecurityAlerts",
                column: "AutoHideAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_ExpiresAt",
                table: "SecurityAlerts",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_IncidentTime",
                table: "SecurityAlerts",
                column: "IncidentTime");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_Latitude_Longitude",
                table: "SecurityAlerts",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_RoutingDecision_CreatedAt",
                table: "SecurityAlerts",
                columns: new[] { "RoutingDecision", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_Status_CreatedAt",
                table: "SecurityAlerts",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_Status_DisplayPriority_AutoHideAt",
                table: "SecurityAlerts",
                columns: new[] { "Status", "DisplayPriority", "AutoHideAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_Status_ReviewPriority_ReviewDueAt",
                table: "SecurityAlerts",
                columns: new[] { "Status", "ReviewPriority", "ReviewDueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_UserId_CreatedAt",
                table: "SecurityAlerts",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_tags_Slug",
                table: "tags",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleId",
                table: "Users",
                column: "GoogleId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertAppeals");

            migrationBuilder.DropTable(
                name: "AlertComments");

            migrationBuilder.DropTable(
                name: "AlertMedia");

            migrationBuilder.DropTable(
                name: "AlertReports");

            migrationBuilder.DropTable(
                name: "AlertVerifications");

            migrationBuilder.DropTable(
                name: "article_views");

            migrationBuilder.DropTable(
                name: "articlecomments");

            migrationBuilder.DropTable(
                name: "ModerationLogs");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "SecurityAlerts");

            migrationBuilder.DropTable(
                name: "articles");

            migrationBuilder.DropTable(
                name: "AlertTypes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "AlertCategories");
        }
    }
}
