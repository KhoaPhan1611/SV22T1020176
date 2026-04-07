using SV22T1020176.BusinessLayers;
using SV22T1020176.Models.Common;
using SV22T1020176.Models.Sales;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SV22T1020176.Admin;

/// <summary>
/// Cung cấp các phương thức hỗ trợ khởi tạo danh sách lựa chọn (SelectList) cho DropDownList trong View.
/// </summary>
public static class SelectListHelper
{
    /// <summary>
    /// Tạo danh sách các Tỉnh/Thành phố hiện có trong hệ thống.
    /// </summary>
    public static async Task<List<SelectListItem>> Provinces()
    {
        var buffer = new List<SelectListItem> { new() { Value = string.Empty, Text = "-- Chọn Tỉnh / Thành phố --" } };
        var source = await DictionaryDataService.ListProvincesAsync();
        
        foreach (var p in source)
        {
            buffer.Add(new SelectListItem { Value = p.ProvinceName, Text = p.ProvinceName });
        }
        return buffer;
    }

    /// <summary>
    /// Tạo danh sách các Loại hàng hóa phục vụ cho việc lọc/chọn dữ liệu.
    /// </summary>
    public static async Task<List<SelectListItem>> Categories()
    {
        var items = new List<SelectListItem> { new() { Value = "0", Text = "-- Toàn bộ loại hàng --" } };
        var queryInput = new PaginationSearchInput { Page = 1, PageSize = 0, SearchValue = string.Empty };
        var resultData = await CatalogDataService.ListCategoriesAsync(queryInput);

        foreach (var category in resultData.DataItems)
        {
            items.Add(new SelectListItem { Value = category.CategoryID.ToString(), Text = category.CategoryName });
        }
        return items;
    }

    /// <summary>
    /// Tạo danh sách các nhà cung ứng chính thức.
    /// </summary>
    public static async Task<List<SelectListItem>> Suppliers()
    {
        var mapping = new List<SelectListItem> { new() { Value = "0", Text = "-- Toàn bộ nhà cung cấp --" } };
        var pagination = new PaginationSearchInput { Page = 1, PageSize = 0, SearchValue = string.Empty };
        var supplierData = await PartnerDataService.ListSuppliersAsync(pagination);

        foreach (var supplier in supplierData.DataItems)
        {
            mapping.Add(new SelectListItem { Value = supplier.SupplierID.ToString(), Text = supplier.SupplierName });
        }
        return mapping;
    }

    /// <summary>
    /// Cung cấp danh sách các trạng thái xử lý của đơn hàng theo bảng mã chuẩn.
    /// </summary>
    public static List<SelectListItem> OrderStatus()
    {
        var statusOptions = new List<SelectListItem> { new() { Value = "", Text = "-- Phân loại trạng thái --" } };
        
        // Nạp các giá trị Enum kèm mô tả (GetDescription)
        var enums = new[] { OrderStatusEnum.New, OrderStatusEnum.Accepted, OrderStatusEnum.Shipping, OrderStatusEnum.Completed, OrderStatusEnum.Rejected, OrderStatusEnum.Cancelled };
        foreach (var s in enums)
        {
            statusOptions.Add(new SelectListItem { Value = s.ToString(), Text = s.GetDescription() });
        }
        
        return statusOptions;
    }
}
