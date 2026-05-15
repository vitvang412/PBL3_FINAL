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
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ── 2. Cấu hình Repositories ──
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();

// ── 3. Cấu hình Services ──
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<AlertLifecycleRuntimeSettings>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddHostedService<AlertLifecycleWorker>();

// ── 4. Cấu hình Authentication (JWT + Google + Cookie) ──
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
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
            var token = context.Request.Cookies["jwtToken"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
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
