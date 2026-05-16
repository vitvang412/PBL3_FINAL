using System.Text;
using DaNangSafeMap.Data;
using DaNangSafeMap.Repositories;
using DaNangSafeMap.Services.Implementations;
using DaNangSafeMap.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
const long maxUploadRequestBytes = 550L * 1024 * 1024;

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxUploadRequestBytes;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxUploadRequestBytes;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// ── 1. Cấu hình Database (MySQL) ──
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 31))));

// ── 2. Cấu hình Repositories ──
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();

// ── 3. Cấu hình Services ──
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<AlertLifecycleRuntimeSettings>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IMissingPersonService, MissingPersonService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<MpChatService>();
builder.Services.AddScoped<ClueService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddHostedService<AlertLifecycleWorker>();

// ── 4. Cấu hình Authentication (JWT + Google + Cookie) ──
builder.Services.AddAuthentication(options =>
{
    // Dùng JWT làm scheme mặc định để [Authorize] đọc jwtToken cookie
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme       = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            // Đọc JWT từ cookie jwtToken (được set bởi login JS) khi request không gửi Bearer token.
            var token = context.Request.Cookies["jwtToken"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        },
        // Khi JWT không hợp lệ → redirect về trang login (thay vì 401 JSON)
        OnChallenge = context =>
        {
            if (!context.Response.HasStarted)
            {
                context.HandleResponse();
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }
                context.Response.Redirect("/Auth/Login");
            }
            return Task.CompletedTask;
        }
    };
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Google:ClientSecret"]!;
});

// ── 5. Cấu hình MVC & Controllers ──
builder.Services.AddControllersWithViews();
builder.Services.AddControllers(); // Cho API Controllers

var app = builder.Build();

// ── Auto-patch DB schema: thêm cột còn thiếu vào Notifications ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Thêm các cột còn thiếu (IF NOT EXISTS → an toàn nếu đã có)
        var sql = @"
            ALTER TABLE Notifications
                ADD COLUMN IF NOT EXISTS Link            VARCHAR(500)   NULL,
                ADD COLUMN IF NOT EXISTS Type            VARCHAR(20)    NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS ArticleId       INT            NULL,
                ADD COLUMN IF NOT EXISTS NotificationType VARCHAR(50)   NOT NULL DEFAULT 'SYSTEM';

            ALTER TABLE clues
                MODIFY COLUMN UserId INT NULL;";
        db.Database.ExecuteSqlRaw(sql);
    }
    catch (Exception ex)
    {
        // Log nhưng không crash app — có thể đã tồn tại
        Console.WriteLine($"[DB patch] {ex.Message}");
    }
}

// ── 6. Cấu hình Pipeline ──
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ── Serve uploads từ ngoài wwwroot (tránh dotnet watch trigger hot reload) ──
// Tất cả file upload (articles, alerts, comments) được lưu vào App_Data/uploads/
// và serve tại URL /uploads/... — dotnet watch không theo dõi App_Data
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ── 7. Map Routes ──
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers(); // Map API routes

app.Run();
