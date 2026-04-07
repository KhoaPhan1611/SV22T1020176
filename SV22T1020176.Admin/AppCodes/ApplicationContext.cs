using Newtonsoft.Json;
using SV22T1020176.Models.Security;

namespace SV22T1020176.Admin;

/// <summary>
/// Lớp tiện ích cung cấp quyền truy cập toàn cục vào ngữ cảnh của ứng dụng web (HttpContext, Environment, Configuration).
/// Hỗ trợ quản lý Session và thông tin người dùng đăng nhập hiện tại.
/// </summary>
public static class ApplicationContext
{
    private static IHttpContextAccessor? _accessor;
    private static IWebHostEnvironment? _environment;
    private static IConfiguration? _config;

    /// <summary>
    /// Thực hiện cấu hình các dịch vụ cốt lõi tại thời điểm ứng dụng khởi động.
    /// </summary>
    public static void Configure(IHttpContextAccessor hca, IWebHostEnvironment whe, IConfiguration ic)
    {
        _accessor = hca ?? throw new ArgumentNullException(nameof(hca));
        _environment = whe ?? throw new ArgumentNullException(nameof(whe));
        _config = ic ?? throw new ArgumentNullException(nameof(ic));
    }

    /// <summary>
    /// Ngữ cảnh HTTP hiện tại của yêu cầu đang xử lý.
    /// </summary>
    public static HttpContext? HttpContext => _accessor?.HttpContext;
    
    /// <summary>
    /// Thông tin về môi trường lưu trữ của ứng dụng.
    /// </summary>
    public static IWebHostEnvironment? WebHostEnviroment => _environment;
    
    /// <summary>
    /// Truy xuất thông tin từ tệp cấu hình của hệ thống.
    /// </summary>
    public static IConfiguration? Configuration => _config;

    /// <summary>
    /// Địa chỉ gốc của trang web hiện tại (VD: https://shop.vn/).
    /// </summary>
    public static string BaseURL => $"{HttpContext?.Request.Scheme}://{HttpContext?.Request.Host}/";
    
    /// <summary>
    /// Đường dẫn vật lý tuyệt đối đến thư mục gốc chứa các tệp tĩnh.
    /// </summary>
    public static string WWWRootPath => _environment?.WebRootPath ?? string.Empty;
    
    /// <summary>
    /// Đường dẫn vật lý tuyệt đối đến thư mục gốc của toàn bộ mã nguồn ứng dụng.
    /// </summary>
    public static string ApplicationRootPath => _environment?.ContentRootPath ?? string.Empty;

    /// <summary>
    /// Nạp dữ liệu vào phiên làm việc dưới dạng chuỗi JSON hóa.
    /// </summary>
    public static void SetSessionData(string key, object val)
    {
        try
        {
            var serializedContent = JsonConvert.SerializeObject(val);
            if (!string.IsNullOrEmpty(serializedContent))
            {
                HttpContext?.Session.SetString(key, serializedContent);
            }
        }
        catch { /* Bỏ qua các sự cố ghi session nếu có */ }
    }

    /// <summary>
    /// Lấy và giải mã dữ liệu từ phiên làm việc dựa trên khóa định danh.
    /// </summary>
    public static T? GetSessionData<T>(string key) where T : class
    {
        try
        {
            var dataInSession = HttpContext?.Session.GetString(key);
            if (!string.IsNullOrEmpty(dataInSession))
            {
                return JsonConvert.DeserializeObject<T>(dataInSession);
            }
        }
        catch { /* Bỏ qua các sự cố đọc session nếu có */ }
        return default;
    }

    /// <summary>
    /// Truy xuất giá trị cấu hình đơn lẻ từ appsettings.json.
    /// </summary>
    public static string GetConfigValue(string name) => _config?[name] ?? string.Empty;

    /// <summary>
    /// Nạp một phần cấu hình phức tạp từ tệp cài đặt vào một lớp đối tượng cụ thể.
    /// </summary>
    public static T PopulateConfigSection<T>(string sectionName) where T : new()
    {
        var sectionObject = new T();
        _config?.GetSection(sectionName).Bind(sectionObject);
        return sectionObject;
    }

    /// <summary>
    /// Truy vấn thực thể WebUser đại diện cho nhân viên đang thực thi các tiến trình hiện tại.
    /// </summary>
    public static WebUser? CurrentUser
    {
        get
        {
            var principalIdentity = HttpContext?.User;
            return (principalIdentity is { Identity.IsAuthenticated: true }) ? new WebUser(principalIdentity) : null;
        }
    }

    /// <summary>
    /// Kiểm tra xem nhân viên hiện tại có sở hữu một quyền hạn nhất định hay không.
    /// </summary>
    public static bool HasPermission(string permissionId) => CurrentUser?.HasPermission(permissionId) ?? false;

    /// <summary>
    /// Xác định xem nhân viên hiện tại có thuộc về một vai trò nhất định hay không.
    /// </summary>
    public static bool HasRole(string roleName) => CurrentUser?.HasRole(roleName) ?? false;
}
