using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SV22T1020176.Shop.Models;
using SV22T1020176.BusinessLayers;
using SV22T1020176.Models.Catalog;
using SV22T1020176.Models.Common;

namespace SV22T1020176.Shop.Controllers;

/// <summary>
/// Bộ điều khiển quản lý giao diện trang chủ (landing page) của cửa hàng trực tuyến và các tính năng
/// tìm kiếm/lọc sản phẩm liên quan. Cấp quyền truy cập đến danh sách sản phẩm với các bộ lọc như
/// danh mục, khoảng giá, từ khóa tìm kiếm, với hỗ trợ phân trang. Tích hợp ajax để người dùng
/// có thể lọc sản phẩm động mà không cần tải lại toàn bộ trang.
/// </summary>
public class HomeController : Controller
{
    private const int DEFAULT_ITEM_COUNT = 12;

    /// <summary>
    /// Chuẩn bị và hiển thị trang chủ chính của cửa hàng với danh sách sản phẩm mặc định được phân trang.
    /// Phương thức truy vấn danh sách các danh mục sản phẩm, xây dựng tiêu chí tìm kiếm dựa trên
    /// tham số từ URL bao gồm trang, danh mục, từ khóa tìm kiếm, và khoảng giá, sau đó truyền
    /// dữ liệu này đến view để hiển thị grid sản phẩm cùng các bộ lọc sidebar.
    /// </summary>
    public async Task<IActionResult> Index(
        int page = 1,
        int categoryID = 0,
        string searchValue = "",
        decimal minPrice = 0,
        decimal maxPrice = 0)
    {
        var categoryList = await CatalogDataService.ListCategoriesAsync(
            new PaginationSearchInput { Page = 1, PageSize = 100 }).ConfigureAwait(false);

        var searchParams = new ProductSearchInput
        {
            Page = Math.Max(1, page),
            PageSize = DEFAULT_ITEM_COUNT,
            SearchValue = searchValue?.Trim() ?? string.Empty,
            CategoryID = Math.Max(0, categoryID),
            MinPrice = Math.Max(0m, minPrice),
            MaxPrice = Math.Max(0m, maxPrice)
        };
        
        var productList = await CatalogDataService.ListProductsAsync(searchParams).ConfigureAwait(false);

        ViewBag.Categories = categoryList;
        ViewBag.ProductResult = productList;
        ViewBag.CurrentCategoryID = categoryID;
        ViewBag.CurrentSearchValue = searchValue;
        ViewBag.CurrentMinPrice = minPrice;
        ViewBag.CurrentMaxPrice = maxPrice;

        return View();
    }

    /// <summary>
    /// Tiếp nhận yêu cầu AJAX từ giao diện người dùng với các tiêu chí lọc như danh mục, từ khóa tìm kiếm,
    /// khoảng giá bắt đầu và kết thúc. Xác thực dữ liệu để đảm bảo giá kết thúc cao hơn giá bắt đầu, 
    /// có tiêu chí giá hợp lệ (không âm). Truy vấn cơ sở dữ liệu với các tiêu chí đã chuẩn hóa và trả về
    /// PartialView chứa grid sản phẩm được lọc để JavaScript cập nhật phần giao diện mà không tải lại trang.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> FilterProducts(ProductSearchInput criteria)
    {
        if ((criteria.MaxPrice > 0 && criteria.MinPrice > 0) && criteria.MaxPrice < criteria.MinPrice)
            criteria.MaxPrice = 0;

        criteria.MinPrice = Math.Max(0, criteria.MinPrice);
        criteria.MaxPrice = Math.Max(0, criteria.MaxPrice);
        criteria.PageSize = DEFAULT_ITEM_COUNT;
        
        var filteredResult = await CatalogDataService.ListProductsAsync(criteria).ConfigureAwait(false);
        return PartialView("_ProductGrid", filteredResult);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return View(new ErrorViewModel { RequestId = requestId });
    }
}
