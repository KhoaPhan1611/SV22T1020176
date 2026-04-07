namespace SV22T1020176.Models.Constants;

/// <summary>
/// Định nghĩa các quyền (Permission) trong hệ thống.
/// Mỗi quyền tương ứng với một hành động cụ thể trên một nhóm dữ liệu.
/// </summary>
public static class Permissions
{
    // ========== Supplier ==========
    /// <summary>Xem danh sách nhà cung cấp</summary>
    public const string SupplierView = "supplier:view";
    /// <summary>Thêm nhà cung cấp</summary>
    public const string SupplierCreate = "supplier:create";
    /// <summary>Sửa nhà cung cấp</summary>
    public const string SupplierEdit = "supplier:edit";
    /// <summary>Xóa nhà cung cấp</summary>
    public const string SupplierDelete = "supplier:delete";

    // ========== Customer ==========
    /// <summary>Xem danh sách khách hàng</summary>
    public const string CustomerView = "customer:view";
    /// <summary>Thêm khách hàng</summary>
    public const string CustomerCreate = "customer:create";
    /// <summary>Sửa khách hàng</summary>
    public const string CustomerEdit = "customer:edit";
    /// <summary>Xóa khách hàng</summary>
    public const string CustomerDelete = "customer:delete";
    /// <summary>Đổi mật khẩu khách hàng</summary>
    public const string CustomerChangePassword = "customer:changepassword";

    // ========== Shipper ==========
    /// <summary>Xem danh sách người giao hàng</summary>
    public const string ShipperView = "shipper:view";
    /// <summary>Thêm người giao hàng</summary>
    public const string ShipperCreate = "shipper:create";
    /// <summary>Sửa người giao hàng</summary>
    public const string ShipperEdit = "shipper:edit";
    /// <summary>Xóa người giao hàng</summary>
    public const string ShipperDelete = "shipper:delete";

    // ========== Employee ==========
    /// <summary>Xem danh sách nhân viên</summary>
    public const string EmployeeView = "employee:view";
    /// <summary>Thêm nhân viên</summary>
    public const string EmployeeCreate = "employee:create";
    /// <summary>Sửa nhân viên</summary>
    public const string EmployeeEdit = "employee:edit";
    /// <summary>Xóa nhân viên</summary>
    public const string EmployeeDelete = "employee:delete";
    /// <summary>Phân quyền nhân viên</summary>
    public const string EmployeeAssignRole = "employee:assignrole";
    /// <summary>Đổi mật khẩu nhân viên</summary>
    public const string EmployeeChangePassword = "employee:changepassword";

    // ========== Category ==========
    /// <summary>Xem danh sách loại hàng</summary>
    public const string CategoryView = "category:view";
    /// <summary>Thêm loại hàng</summary>
    public const string CategoryCreate = "category:create";
    /// <summary>Sửa loại hàng</summary>
    public const string CategoryEdit = "category:edit";
    /// <summary>Xóa loại hàng</summary>
    public const string CategoryDelete = "category:delete";

    // ========== Product ==========
    /// <summary>Xem danh sách mặt hàng</summary>
    public const string ProductView = "product:view";
    /// <summary>Thêm mặt hàng</summary>
    public const string ProductCreate = "product:create";
    /// <summary>Sửa mặt hàng</summary>
    public const string ProductEdit = "product:edit";
    /// <summary>Xóa mặt hàng</summary>
    public const string ProductDelete = "product:delete";
    /// <summary>Quản lý ảnh mặt hàng</summary>
    public const string ProductManagePhoto = "product:managephoto";
    /// <summary>Quản lý thuộc tính mặt hàng</summary>
    public const string ProductManageAttribute = "product:manageattribute";

    // ========== Order ==========
    /// <summary>Xem danh sách đơn hàng</summary>
    public const string OrderView = "order:view";
    /// <summary>Lập đơn hàng</summary>
    public const string OrderCreate = "order:create";
    /// <summary>Xem chi tiết đơn hàng</summary>
    public const string OrderDetail = "order:detail";
    /// <summary>Duyệt đơn hàng</summary>
    public const string OrderAccept = "order:accept";
    /// <summary>Từ chối đơn hàng</summary>
    public const string OrderReject = "order:reject";
    /// <summary>Hủy đơn hàng</summary>
    public const string OrderCancel = "order:cancel";
    /// <summary>Giao đơn hàng cho shipper</summary>
    public const string OrderShip = "order:ship";
    /// <summary>Hoàn tất đơn hàng</summary>
    public const string OrderComplete = "order:complete";
    /// <summary>Xóa đơn hàng</summary>
    public const string OrderDelete = "order:delete";

    // ========== Dashboard ==========
    /// <summary>Xem Dashboard thống kê</summary>
    public const string DashboardView = "dashboard:view";
}
