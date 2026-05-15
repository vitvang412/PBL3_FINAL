using Microsoft.EntityFrameworkCore;
using DaNangSafeMap.Models.Entities;

namespace DaNangSafeMap.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ── Bảng Users (đã có) ──
        public DbSet<User> Users { get; set; }

        // ── Các bảng cho MissingPerson, Chat, Report ──
        public DbSet<MissingPerson> MissingPersons { get; set; }
        public DbSet<Clue> Clues { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Report> Reports { get; set; }

        // ── Bảng Alert (MỚI) ──
        public DbSet<AlertCategory> AlertCategories { get; set; }
        public DbSet<AlertType> AlertTypes { get; set; }
        public DbSet<SecurityAlert> SecurityAlerts { get; set; }
        public DbSet<AlertMedia> AlertMedia { get; set; }
        public DbSet<AlertVerification> AlertVerifications { get; set; }
        public DbSet<ModerationLog> ModerationLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AlertAppeal> AlertAppeals { get; set; }
        public DbSet<AlertReport> AlertReports { get; set; }
        public DbSet<AlertComment> AlertComments { get; set; }

        // ── Bảng Article (MỚI) ──
        public DbSet<Category> Categories { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<ArticleComment> ArticleComments { get; set; }
        public DbSet<ArticleView> ArticleViews { get; set; }
        public DbSet<Tag> Tags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ════════════════════════════════════════════
            // USER (giữ nguyên config cũ)
            // ════════════════════════════════════════════
            modelBuilder.Entity<User>(entity =>
            {
                // Unique constraints
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.GoogleId).IsUnique();

                // Default values
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.Property(e => e.Role)
                      .HasDefaultValue("User");

                entity.Property(e => e.AuthProvider)
                      .HasDefaultValue("Local");

                entity.Property(e => e.IsActive)
                      .HasDefaultValue(true);

                entity.Property(e => e.IsBanned)
                      .HasDefaultValue(false);

                entity.Property(e => e.ReputationScore)
                      .HasDefaultValue(5);
            });

            // ════════════════════════════════════════════
            // ALERT CATEGORY
            // ════════════════════════════════════════════
            modelBuilder.Entity<AlertCategory>(entity =>
            {
                entity.HasIndex(e => e.Slug).IsUnique();

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.Property(e => e.IsActive)
                      .HasDefaultValue(true);

                // 1 Category → nhiều AlertType
                entity.HasMany(e => e.AlertTypes)
                      .WithOne(t => t.Category)
                      .HasForeignKey(t => t.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ════════════════════════════════════════════
            // ALERT TYPE
            // ════════════════════════════════════════════
            modelBuilder.Entity<AlertType>(entity =>
            {
                entity.HasIndex(e => e.Slug).IsUnique();

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.Property(e => e.IsActive)
                      .HasDefaultValue(true);

                // 1 AlertType → nhiều SecurityAlert
                entity.HasMany(e => e.SecurityAlerts)
                      .WithOne(a => a.AlertType)
                      .HasForeignKey(a => a.AlertTypeId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ════════════════════════════════════════════
            // SECURITY ALERT (bảng chính)
            // ════════════════════════════════════════════
            modelBuilder.Entity<SecurityAlert>(entity =>
            {
                // Default values
                entity.Property(e => e.Status)
                      .HasDefaultValue("PENDING_REVIEW");

                entity.Property(e => e.TrustScore)
                      .HasDefaultValue(0);

                entity.Property(e => e.RoutingDecision)
                      .HasDefaultValue("GREEN");

                entity.Property(e => e.ConfirmCount)
                      .HasDefaultValue(0);

                entity.Property(e => e.DenyCount)
                      .HasDefaultValue(0);

                entity.Property(e => e.Opacity)
                      .HasDefaultValue(30);

                entity.Property(e => e.ReviewPriority)
                      .HasDefaultValue("NORMAL");

                entity.Property(e => e.DisplayPriority)
                      .HasDefaultValue(50);

                entity.Property(e => e.HasMedia)
                      .HasDefaultValue(false);

                entity.Property(e => e.UserConfirmed)
                      .HasDefaultValue(false);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.Property(e => e.UpdatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                // FK → User
                entity.HasOne(e => e.User)
                      .WithMany(u => u.SecurityAlerts)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // 1 Alert → nhiều Media
                entity.HasMany(e => e.Media)
                      .WithOne(m => m.Alert)
                      .HasForeignKey(m => m.AlertId)
                      .OnDelete(DeleteBehavior.Cascade);

                // 1 Alert → nhiều Verification
                entity.HasMany(e => e.Verifications)
                      .WithOne(v => v.Alert)
                      .HasForeignKey(v => v.AlertId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Indexes tối ưu cho bản đồ
                entity.HasIndex(e => new { e.Latitude, e.Longitude });
                entity.HasIndex(e => new { e.Status, e.CreatedAt });
                entity.HasIndex(e => new { e.UserId, e.CreatedAt });
                entity.HasIndex(e => new { e.RoutingDecision, e.CreatedAt });
                entity.HasIndex(e => new { e.Status, e.ReviewPriority, e.ReviewDueAt });
                entity.HasIndex(e => new { e.Status, e.DisplayPriority, e.AutoHideAt });
                entity.HasIndex(e => e.IncidentTime);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.AutoHideAt);
            });

            // ════════════════════════════════════════════
            // ALERT MEDIA
            // ════════════════════════════════════════════
            modelBuilder.Entity<AlertMedia>(entity =>
            {
                entity.Property(e => e.MediaType)
                      .HasDefaultValue("IMAGE");

                entity.Property(e => e.SourceType)
                      .HasDefaultValue("ORIGINAL");

                entity.Property(e => e.IsActive)
                      .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                // FK → User
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Index
                entity.HasIndex(e => e.AlertId);
            });

            // ════════════════════════════════════════════
            // ALERT VERIFICATION
            // ════════════════════════════════════════════
            modelBuilder.Entity<AlertVerification>(entity =>
            {
                entity.HasIndex(e => new { e.AlertId, e.UserId }).IsUnique();

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.AlertId, e.VerificationType });
            });

            modelBuilder.Entity<ModerationLog>(entity =>
            {
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.Alert)
                      .WithMany(a => a.ModerationLogs)
                      .HasForeignKey(e => e.AlertId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AdminUser)
                      .WithMany(u => u.ModerationLogs)
                      .HasForeignKey(e => e.AdminUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.AlertId, e.CreatedAt });
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.Property(e => e.IsRead)
                      .HasDefaultValue(false);

                entity.Property(e => e.NotificationType)
                      .HasDefaultValue("SYSTEM");

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.User)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Alert)
                      .WithMany(a => a.Notifications)
                      .HasForeignKey(e => e.AlertId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Article)
                      .WithMany()
                      .HasForeignKey(e => e.ArticleId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt });
            });

            modelBuilder.Entity<AlertAppeal>(entity =>
            {
                entity.Property(e => e.Status)
                      .HasDefaultValue("PENDING");

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.Alert)
                      .WithMany(a => a.Appeals)
                      .HasForeignKey(e => e.AlertId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.AlertAppeals)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ReviewedByAdmin)
                      .WithMany()
                      .HasForeignKey(e => e.ReviewedByAdminId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.AlertId, e.Status, e.CreatedAt });
            });

            modelBuilder.Entity<AlertReport>(entity =>
            {
                entity.Property(e => e.Status)
                      .HasDefaultValue("PENDING");

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.Alert)
                      .WithMany(a => a.Reports)
                      .HasForeignKey(e => e.AlertId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Reporter)
                      .WithMany(u => u.AlertReports)
                      .HasForeignKey(e => e.ReporterId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.AlertId, e.ReporterId }).IsUnique();
                entity.HasIndex(e => new { e.Status, e.CreatedAt });
            });

            // ════════════════════════════════════════════
            // ALERT COMMENT
            // ════════════════════════════════════════════
            modelBuilder.Entity<AlertComment>(entity =>
            {
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.Alert)
                      .WithMany(a => a.Comments)
                      .HasForeignKey(e => e.AlertId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.AlertId, e.CreatedAt });
            });

            // ════════════════════════════════════════════
            // CATEGORY (Tin tức)
            // ════════════════════════════════════════════
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasIndex(e => e.Slug).IsUnique();
            });

            // ════════════════════════════════════════════
            // ARTICLE
            // ════════════════════════════════════════════
            modelBuilder.Entity<Article>(entity =>
            {
                entity.HasIndex(e => e.Slug).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.Property(e => e.Status).HasDefaultValue(1);
                entity.Property(e => e.IsFeatured).HasDefaultValue(false);
                entity.Property(e => e.ViewCount).HasDefaultValue(0);
                entity.Property(e => e.CategoryId).HasDefaultValue(1);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.Category)
                      .WithMany(c => c.Articles)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Author)
                      .WithMany(u => u.Articles)
                      .HasForeignKey(e => e.AuthorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Moderator)
                      .WithMany()
                      .HasForeignKey(e => e.ModeratedBy)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ════════════════════════════════════════════
            // ARTICLE COMMENT
            // ════════════════════════════════════════════
            modelBuilder.Entity<ArticleComment>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.Article)
                      .WithMany(a => a.Comments)
                      .HasForeignKey(e => e.ArticleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.ArticleComments)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ════════════════════════════════════════════
            // ARTICLE VIEW
            // ════════════════════════════════════════════
            modelBuilder.Entity<ArticleView>(entity =>
            {
                entity.Property(e => e.ViewedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.HasOne(e => e.Article)
                      .WithMany(a => a.Views)
                      .HasForeignKey(e => e.ArticleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ════════════════════════════════════════════
            // TAG
            // ════════════════════════════════════════════
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasIndex(e => e.Slug).IsUnique();
            });
        }
    }
}
