using Microsoft.AspNetCore.Mvc;
using SV22T1020176.BusinessLayers;
using SV22T1020176.Models.Catalog;
using SV22T1020176.Models.Common;

namespace SV22T1020176.Shop.Controllers;

/// <summary>
/// Bộ điều khiển quản lý hiển thị danh sách sản phẩm, tìm kiếm/lọc sản phẩm dựa trên nhiều tiêu chí
/// bao gồm danh mục, từ khóa, khoảng giá, hỗ trợ phân trang, và xem chi tiết từng sản phẩm cụ thể.
/// Cấp quyền truy cập thông tin bổ trợ như hình ảnh sản phẩm, thuộc tính chi tiết,
/// thông tin nhà cung cấp, và danh mục liên quan.
/// </summary>
public class ProductController : Controller
{
    private const int ITEMS_PER_PAGE_DEFAULT = 12;

    /// <summary>
    /// Hiển thị trang liệt kê sản phẩm với giao diện chứa grid/danh sách sản phẩm và các bộ lọc sidebar.
    /// Phương thức xây dựng bộ tiêu chí tìm kiếm từ các tham số URL bao gồm trang hiện tại, danh mục
    /// đã chọn, từ khóa tìm kiếm, khoảng giá tối thiểu và tối đa. Truy vấn danh sách danh mục để
    /// hiển thị trong dropdown lọc, lưu trữ các tiêu chí lọc hiện tại vào ViewBag để ghi nhớ khi
    /// người dùng quay lại trang này.
    /// </summary>
    public async Task<IActionResult> Index(int page = 1, int categoryID = 0, string searchValue = "", decimal minPrice = 0, decimal maxPrice = 0)
    {
        var filterOptions = new ProductSearchInput
        {
            Page = Math.Max(1, page),
            PageSize = ITEMS_PER_PAGE_DEFAULT,
            CategoryID = Math.Max(0, categoryID),
            SearchValue = searchValue?.Trim() ?? string.Empty,
            MinPrice = Math.Max(0m, minPrice),
            MaxPrice = Math.Max(0m, maxPrice)
        };

        var categoryData = await CatalogDataService.ListCategoriesAsync(
            new PaginationSearchInput { Page = 1, PageSize = 100 }).ConfigureAwait(false);
        
        ViewBag.Categories = categoryData;
        ViewBag.CurrentCategoryID = categoryID;
        ViewBag.CurrentSearchValue = searchValue;
        ViewBag.CurrentMinPrice = filterOptions.MinPrice;
        ViewBag.CurrentMaxPrice = filterOptions.MaxPrice;
        
        return View(filterOptions);
    }

    /// <summary>
    /// Tiếp nhận yêu cầu AJAX từ giao diện người dùng với bộ tiêu chí lọc sản phẩm như danh mục,
    /// từ khóa tìm kiếm, và khoảng giá. Xác thực dữ liệu để đảm bảo giá kết thúc không thấp hơn
    /// giá bắt đầu, chuẩn hóa các giá trị giá tiền (không âm). Truy vấn cơ sở dữ liệu với tiêu chí
    /// đã xác thực và trả về PartialView chứa grid sản phẩm được lọc để JavaScript cập nhật
    /// phần tử DOM trên trang mà không cần tải lại toàn bộ.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProductList(ProductSearchInput filter)
    {
        if (filter.MaxPrice > 0 && filter.MinPrice > 0 && filter.MaxPrice < filter.MinPrice)
            filter.MaxPrice = 0;

        filter.MinPrice = Math.Max(0, filter.MinPrice);
        filter.MaxPrice = Math.Max(0, filter.MaxPrice);
        filter.PageSize = ITEMS_PER_PAGE_DEFAULT;

        var queryResult = await CatalogDataService.ListProductsAsync(filter).ConfigureAwait(false);
        return PartialView("_ProductGrid", queryResult);
    }

    /// <summary>
    /// Truy vấn và hiển thị thông tin chi tiết toàn diện của một sản phẩm cụ thể từ cơ sở dữ liệu
    /// bao gồm tên, giá bán, mô tả, trạng thái kinh doanh. Tích hợp các dữ liệu bổ trợ như danh
    /// sách hình ảnh sản phẩm để hiển thị gallery, các thuộc tính chi tiết (kích thước, màu sắc...),
    /// thông tin nhà cung cấp (nếu có) và danh mục sản phẩm (nếu có) để cung cấp bối cảnh đầy đủ.
    /// Nếu sản phẩm không tồn tại, chuyển hướng về trang liệt kê sản phẩm.
    /// </summary>
    public async Task<IActionResult> Detail(int id)
    {
        var productData = await CatalogDataService.GetProductAsync(id).ConfigureAwait(false);
        
        if (productData is null)
            return RedirectToAction(nameof(Index));

        var imgCollection = await CatalogDataService.ListPhotosAsync(id).ConfigureAwait(false);
        var attrCollection = await CatalogDataService.ListAttributesAsync(id).ConfigureAwait(false);
        
        ViewBag.Photos = imgCollection;
        ViewBag.Attributes = attrCollection;

        if (productData.SupplierID.HasValue && productData.SupplierID > 0)
        {
            var supplierData = await PartnerDataService.GetSupplierAsync(productData.SupplierID.Value).ConfigureAwait(false);
            ViewBag.Supplier = supplierData;
        }

        if (productData.CategoryID.HasValue && productData.CategoryID > 0)
        {
            var categoryData = await CatalogDataService.GetCategoryAsync(productData.CategoryID.Value).ConfigureAwait(false);
            ViewBag.Category = categoryData;
        }

        return View(productData);
    }
}
