using System.Security.Claims;
using SV22T1020176.Models.Constants;

namespace SV22T1020176.Models.Security;

/// <summary>
/// Lớp đại diện cho người dùng hiện tại, đọc thông tin từ ClaimsPrincipal.
/// Cung cấp các phương thức tiện ích để kiểm tra quyền.
/// </summary>
public class WebUser
{
    private readonly ClaimsPrincipal _principal;
    private List<string>? _permissions;

    public WebUser(ClaimsPrincipal principal)
    {
        _principal = principal ?? throw new ArgumentNullException(nameof(principal));
    }

    /// <summary>Mã nhân viên</summary>
    public int EmployeeID => int.TryParse(GetClaimValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    /// <summary>Họ tên</summary>
    public string FullName => GetClaimValue(ClaimTypes.Name) ?? "";

    /// <summary>Email</summary>
    public string Email => GetClaimValue(ClaimTypes.Email) ?? "";

    /// <summary>Danh sách vai trò (phân cách bởi dấu phẩy)</summary>
    public string RoleNames => GetClaimValue(ClaimTypes.Role) ?? "";

    /// <summary>Đường dẫn ảnh đại diện</summary>
    public string? Photo => GetClaimValue("Photo");

    /// <summary>
    /// Danh sách các vai trò dưới dạng List
    /// </summary>
    public List<string> Roles
    {
        get
        {
            if (string.IsNullOrWhiteSpace(RoleNames))
                return new List<string>();
            return RoleNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
    }

    /// <summary>
    /// Danh sách tất cả các quyền (Permissions) mà user có,
    /// bao gồm cả quyền kế thừa từ các vai trò.
    /// </summary>
    public List<string> Permissions
    {
        get
        {
            if (_permissions != null)
                return _permissions;

            _permissions = new List<string>();
            foreach (var role in Roles)
            {
                var perms = Constants.Roles.GetPermissions(role);
                foreach (var perm in perms)
                {
                    if (!_permissions.Contains(perm))
                        _permissions.Add(perm);
                }
            }
            return _permissions;
        }
    }

    /// <summary>
    /// Kiểm tra xem user có một quyền cụ thể hay không.
    /// </summary>
    /// <param name="permission">Mã quyền (VD: "product:view")</param>
    public bool HasPermission(string permission)
    {
        return Permissions.Contains(permission);
    }

    /// <summary>
    /// Kiểm tra xem user có bất kỳ quyền nào trong danh sách hay không.
    /// </summary>
    /// <param name="permissions">Danh sách quyền cần kiểm tra</param>
    public bool HasAnyPermission(params string[] permissions)
    {
        return permissions.Any(p => Permissions.Contains(p));
    }

    /// <summary>
    /// Kiểm tra xem user có tất cả các quyền trong danh sách hay không.
    /// </summary>
    /// <param name="permissions">Danh sách quyền cần kiểm tra</param>
    public bool HasAllPermissions(params string[] permissions)
    {
        return permissions.All(p => Permissions.Contains(p));
    }

    /// <summary>
    /// Kiểm tra xem user có một vai trò cụ thể hay không.
    /// </summary>
    /// <param name="role">Tên vai trò</param>
    public bool HasRole(string role)
    {
        return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Kiểm tra xem user có vai trò Admin hay không (toàn quyền).
    /// </summary>
    public bool IsAdmin => HasRole(Constants.Roles.Admin);

    /// <summary>
    /// Kiểm tra xem user có đăng nhập hay chưa.
    /// </summary>
    public bool IsAuthenticated => _principal.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// Kiểm tra xem user có thể thao tác trên đối tượng có OwnerID hay không.
    /// Chỉ Admin hoặc chính chủ mới được thao tác.
    /// </summary>
    /// <param name="ownerId">Mã chủ sở hữu (thường là EmployeeID)</param>
    public bool CanAccess(int ownerId)
    {
        return IsAdmin || EmployeeID == ownerId;
    }

    private string? GetClaimValue(string claimType)
    {
        return _principal.FindFirst(claimType)?.Value;
    }
}

/// <summary>
/// Extension methods để tạo WebUser từ ClaimsPrincipal
/// </summary>
public static class WebUserExtensions
{
    /// <summary>
    /// Tạo WebUser từ ClaimsPrincipal
    /// </summary>
    public static WebUser GetWebUser(this ClaimsPrincipal principal)
    {
        return new WebUser(principal);
    }
}
