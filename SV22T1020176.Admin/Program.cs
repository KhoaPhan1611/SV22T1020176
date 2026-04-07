using SV22T1020176.Admin;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;
using SV22T1020176.Models.Constants;
using SV22T1020176.Admin.AppCodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Data Protection keys persist across app restarts ──────────────────
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
Directory.CreateDirectory(keysFolder);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("SV22T1020176.Admin")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(365)); // key sống 1 năm

// ── 2. HTTP Context & MVC ─────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews()
    .AddMvcOptions(option =>
    {
        option.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });

// ── 3. Cookie Authentication ─────────────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(option =>
    {
        option.Cookie.Name = "SV22T1020176.Admin";
        option.LoginPath = "/Account/Login";
        option.AccessDeniedPath = "/Account/AccessDenied";
        option.ExpireTimeSpan = TimeSpan.FromDays(30);      // cookie sống 30 ngày tuyệt đối
        option.SlidingExpiration = true;                    // reset timer mỗi lần dùng
        option.Cookie.HttpOnly = true;
        option.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        // IMPORTANT: đặt same site = lax để refresh tab không bị丢失 cookie
        option.Cookie.SameSite = SameSiteMode.Lax;
    });

// ── 4. Authorization ───────────────────────────────────────────────────────
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

// Dùng dynamic policy provider để hỗ trợ combined policies (OR-logic) mà không cần đăng ký trước
builder.Services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(Roles.Admin));

    options.AddPolicy("ManagerOrAdmin", policy =>
        policy.RequireRole(Roles.Admin, Roles.Manager));
    // Các policy Permission_* được tạo động bởi DynamicPermissionPolicyProvider
});

// ── 5. Session ────────────────────────────────────────────────────────────
builder.Services.AddSession(option =>
{
    option.IdleTimeout = TimeSpan.FromHours(2);   // session timeout 2h nếu không dùng
    option.Cookie.HttpOnly = true;
    option.Cookie.IsEssential = true;
    option.Cookie.Name = "SV22T1020176.Admin.Session";
    option.Cookie.SameSite = SameSiteMode.Lax;
    option.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

// ── 6. Middleware order (CRITICAL) ─────────────────────────────────────────
// Session phải đứng TRƯỚC Authentication để HttpContext.User được hydrate từ session
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();
app.UseRouting();

// Session trước Authentication
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ── 7. Routing ────────────────────────────────────────────────────────────
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ── 8. Culture ─────────────────────────────────────────────────────────────
var cultureInfo = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// ── 9. Application Context ────────────────────────────────────────────────
ApplicationContext.Configure
(
    app.Services.GetRequiredService<IHttpContextAccessor>(),
    app.Services.GetRequiredService<IWebHostEnvironment>(),
    app.Configuration
);

string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB")
    ?? throw new InvalidOperationException("ConnectionString 'LiteCommerceDB' not found.");

SV22T1020176.BusinessLayers.Configuration.Initialize(connectionString);

app.Run();
