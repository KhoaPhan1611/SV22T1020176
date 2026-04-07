using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using System.Globalization;
using SV22T1020176.BusinessLayers;

var builder = WebApplication.CreateBuilder(args);

// Data Protection: lưu key vào thư mục cố định để TempData / cookie không lỗi "key was not found in the key ring" sau khi restart
var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .SetApplicationName("SV22T1020176.Shop")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews()
                .AddMvcOptions(option =>
                {
                    option.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                });

// Configure Authentication (Shop Scheme)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(option =>
                {
                    option.Cookie.Name = "SV22T1020176.Shop";
                    option.LoginPath = "/Account/Login";
                    option.AccessDeniedPath = "/Account/AccessDenied";
                    option.ExpireTimeSpan = TimeSpan.FromDays(30);
                    option.SlidingExpiration = true;
                    option.Cookie.HttpOnly = true;
                    option.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                });

// Configure Session
builder.Services.AddSession(option =>
{
    option.IdleTimeout = TimeSpan.FromHours(2);
    option.Cookie.HttpOnly = true;
    option.Cookie.IsEssential = true;
    option.Cookie.Name = "SV22T1020176.Session";
});

// Initialize Business Layer Configuration (IMPORTANT: MUST BE BEFORE app = builder.Build())
string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB")
    ?? throw new InvalidOperationException("ConnectionString 'LiteCommerceDB' not found.");
SV22T1020176.BusinessLayers.Configuration.Initialize(connectionString);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

// Ảnh sản phẩm trong DB là đường dẫn tương đối dưới /images/products/...
// Shop không có bản copy wwwroot/images — dùng chung thư mục với Admin trong cùng solution (logic only, không đổi view).
var adminProductImagesPath = Path.GetFullPath(Path.Combine(
    app.Environment.ContentRootPath,
    "..", "SV22T1020176.Admin", "wwwroot", "images", "products"));
if (Directory.Exists(adminProductImagesPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(adminProductImagesPath),
        RequestPath = "/images/products"
    });
}
else
{
    app.Logger.LogWarning(
        "Không tìm thấy thư mục ảnh sản phẩm tại {Path}. Sao chép wwwroot/images/products từ Admin vào Shop hoặc giữ 2 project cạnh nhau.",
        adminProductImagesPath);
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Configure default format
var cultureInfo = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
