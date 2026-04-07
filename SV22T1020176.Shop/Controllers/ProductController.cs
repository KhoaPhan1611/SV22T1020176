using Microsoft.AspNetCore.Mvc;
using SV22T1020176.BusinessLayers;
using SV22T1020176.Models.Catalog;
using SV22T1020176.Models.Common;

namespace SV22T1020176.Shop.Controllers;

/// <summary>
/// Controller điều hướng các hành động liên quan đến sản phẩm và chi tiết sản phẩm.
/// </summary>
public class ProductController : Controller
{
    private const int ITEMS_PER_PAGE_DEFAULT = 12;

    /// <summary>
    /// Trang liệt kê sản phẩm với các bộ lọc cơ bản.
    /// </summary>
    public async Task<IActionResult> Index(int page = 1, int categoryID = 0, string searchValue = "", decimal minPrice = 0, decimal maxPrice = 0)
    {
        var filter = new ProductSearchInput
        {
            Page = page,
            PageSize = ITEMS_PER_PAGE_DEFAULT,
            CategoryID = categoryID,
            SearchValue = searchValue,
            MinPrice = minPrice,
            MaxPrice = maxPrice
        };

        // Nạp danh sách danh mục để đổ vào dropdown
        var categoriesResponse = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 100 });
        ViewBag.Categories = categoriesResponse;
        ViewBag.CurrentCategoryID = categoryID;
        ViewBag.CurrentSearchValue = searchValue;
        ViewBag.CurrentMinPrice = filter.MinPrice;
        ViewBag.CurrentMaxPrice = filter.MaxPrice;
        
        return View(filter);
    }

    /// <summary>
    /// Xử lý yêu cầu tìm kiếm sản phẩm thông qua AJAX.
    /// Trả về một phần giao diện (PartialView) chứa lưới sản phẩm.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProductList(ProductSearchInput filter)
    {
        // Kiểm tra tính hợp lệ của khoảng giá đầu cuối
        if (filter.MaxPrice > 0 && filter.MinPrice > 0 && filter.MaxPrice < filter.MinPrice)
        {
            filter.MaxPrice = 0;
        }
        
        // Chuẩn hóa dữ giá tiền (không âm)
        filter.MinPrice = filter.MinPrice < 0 ? 0 : filter.MinPrice;
        filter.MaxPrice = filter.MaxPrice < 0 ? 0 : filter.MaxPrice;

        filter.PageSize = ITEMS_PER_PAGE_DEFAULT;
        
        var searchResult = await CatalogDataService.ListProductsAsync(filter);
        return PartialView("_ProductGrid", searchResult);
    }

    /// <summary>
    /// Hiển thị thông tin chi tiết của một sản phẩm cụ thể.
    /// </summary>
    public async Task<IActionResult> Detail(int id)
    {
        var itemDetail = await CatalogDataService.GetProductAsync(id);
        if (itemDetail == null) 
        {
            return RedirectToAction("Index");
        }

        // Tích hợp dữ liệu bổ trợ: Hình ảnh và Thuộc tính
        ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
        ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);
        
        // Truy vấn thông tin nhà cung cấp (Supplier) nếu có
        if (itemDetail.SupplierID.GetValueOrDefault() > 0)
        {
            var supplierInfo = await PartnerDataService.GetSupplierAsync(itemDetail.SupplierID.Value);
            ViewBag.Supplier = supplierInfo;
        }
            
        // Truy vấn thông tin phân loại (Category) nếu có
        if (itemDetail.CategoryID.GetValueOrDefault() > 0)
        {
            var categoryInfo = await CatalogDataService.GetCategoryAsync(itemDetail.CategoryID.Value);
            ViewBag.Category = categoryInfo;
        }

        return View(itemDetail);
    }
}
