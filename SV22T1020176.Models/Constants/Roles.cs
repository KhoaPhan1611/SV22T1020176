namespace SV22T1020176.Models.Constants;

/// <summary>
/// Định nghĩa các vai trò (Role) mặc định trong hệ thống.
/// Mỗi Role bao gồm một tập hợp các Permissions.
/// </summary>
public static class Roles
{
    // ========== Vai trò hệ thống ==========

    /// <summary>Quản trị viên - toàn quyền trên hệ thống</summary>
    public const string Admin = "Admin";

    /// <summary>Nhân viên quản lý - quản lý toàn bộ dữ liệu nghiệp vụ</summary>
    public const string Manager = "Manager";

    /// <summary>Nhân viên bán hàng - chỉ thao tác trên đơn hàng</summary>
    public const string Sales = "Sales";

    /// <summary>Nhân viên kho - quản lý hàng hóa</summary>
    public const string Inventory = "Inventory";



    // ========== Tập hợp Permissions theo Role ==========

    /// <summary>
    /// Trả về danh sách Permissions mặc định cho mỗi Role.
    /// Format: "role:permissions" (phân cách bởi dấu phẩy)
    /// </summary>
    public static readonly Dictionary<string, string[]> RolePermissions = new()
    {
        {
            Admin, new[]
            {
                // Toàn quyền
                Permissions.DashboardView,
                Permissions.SupplierView, Permissions.SupplierCreate, Permissions.SupplierEdit, Permissions.SupplierDelete,
                Permissions.CustomerView, Permissions.CustomerCreate, Permissions.CustomerEdit, Permissions.CustomerDelete, Permissions.CustomerChangePassword,
                Permissions.ShipperView, Permissions.ShipperCreate, Permissions.ShipperEdit, Permissions.ShipperDelete,
                Permissions.EmployeeView, Permissions.EmployeeCreate, Permissions.EmployeeEdit, Permissions.EmployeeDelete, Permissions.EmployeeAssignRole, Permissions.EmployeeChangePassword,
                Permissions.CategoryView, Permissions.CategoryCreate, Permissions.CategoryEdit, Permissions.CategoryDelete,
                Permissions.ProductView, Permissions.ProductCreate, Permissions.ProductEdit, Permissions.ProductDelete, Permissions.ProductManagePhoto, Permissions.ProductManageAttribute,
                Permissions.OrderView, Permissions.OrderCreate, Permissions.OrderDetail, Permissions.OrderAccept,
                Permissions.OrderReject, Permissions.OrderCancel, Permissions.OrderShip, Permissions.OrderComplete, Permissions.OrderDelete,
            }
        },
        {
            Manager, new[]
            {
                Permissions.DashboardView,
                Permissions.SupplierView, Permissions.SupplierCreate, Permissions.SupplierEdit,
                Permissions.CustomerView, Permissions.CustomerCreate, Permissions.CustomerEdit,
                Permissions.ShipperView, Permissions.ShipperCreate, Permissions.ShipperEdit,
                Permissions.CategoryView, Permissions.CategoryCreate, Permissions.CategoryEdit,
                Permissions.ProductView, Permissions.ProductCreate, Permissions.ProductEdit, Permissions.ProductManagePhoto, Permissions.ProductManageAttribute,
                Permissions.OrderView, Permissions.OrderCreate, Permissions.OrderDetail, Permissions.OrderAccept,
                Permissions.OrderReject, Permissions.OrderCancel, Permissions.OrderShip, Permissions.OrderComplete,
            }
        },
        {
            Sales, new[]
            {
                Permissions.DashboardView,
                Permissions.CustomerView, Permissions.CustomerCreate, Permissions.CustomerEdit,
                Permissions.OrderView, Permissions.OrderCreate, Permissions.OrderDetail, Permissions.OrderAccept,
                Permissions.OrderReject, Permissions.OrderCancel, Permissions.OrderShip, Permissions.OrderComplete,
            }
        },
        {
            Inventory, new[]
            {
                Permissions.DashboardView,
                Permissions.SupplierView, Permissions.SupplierCreate, Permissions.SupplierEdit,
                Permissions.CategoryView, Permissions.CategoryCreate, Permissions.CategoryEdit, Permissions.CategoryDelete,
                Permissions.ProductView, Permissions.ProductCreate, Permissions.ProductEdit, Permissions.ProductDelete, Permissions.ProductManagePhoto, Permissions.ProductManageAttribute,
                Permissions.OrderView, Permissions.OrderDetail,
            }
        },
    };

    /// <summary>
    /// Lấy danh sách tất cả các Permissions duy nhất trong hệ thống.
    /// </summary>
    public static IEnumerable<string> GetAllPermissions()
    {
        return RolePermissions.Values.SelectMany(p => p).Distinct();
    }

    /// <summary>
    /// Kiểm tra xem một Role có tồn tại hay không (không phân biệt hoa thường).
    /// </summary>
    public static bool IsValidRole(string roleName)
    {
        return RolePermissions.Keys.Any(k => k.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Lấy danh sách Permissions của một Role (không phân biệt hoa thường).
    /// </summary>
    public static string[] GetPermissions(string roleName)
    {
        var key = RolePermissions.Keys.FirstOrDefault(k => k.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        return key != null ? RolePermissions[key] : Array.Empty<string>();
    }
}
