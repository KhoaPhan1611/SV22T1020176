using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SV22T1020176.Shop.Models;
using SV22T1020176.BusinessLayers;
using SV22T1020176.Models.Catalog;
using SV22T1020176.Models.Common;

namespace SV22T1020176.Shop.Controllers;

/// <summary>
/// Controller xử lý các luồng chính của trang chủ và tìm kiếm sản phẩm.
/// </summary>
public class HomeController : Controller
{
    private const int DEFAULT_ITEM_COUNT = 12;

    /// <summary>
    /// Hiển thị giao diện chính của cửa hàng (Landing page).
    /// </summary>
    public async Task<IActionResult> Index(
        int page = 1,
        int categoryID = 0,
        string searchValue = "",
        decimal minPrice = 0,
        decimal maxPrice = 0)
    {
        // Thu thập danh sách phân loại (Category) cho thanh lọc
        var filterCategories = await CatalogDataService.ListCategoriesAsync(
            new PaginationSearchInput { 
                Page = 1, 
                PageSize = 100 
            });

        // Xây dựng tiêu chí tìm kiếm sản phẩm dựa trên input người dùng
        var searchInput = new ProductSearchInput
        {
            Page = page,
            PageSize = DEFAULT_ITEM_COUNT,
            SearchValue = searchValue ?? string.Empty,
            CategoryID = categoryID,
            MinPrice = minPrice,
            MaxPrice = maxPrice
        };
        
        var productResult = await CatalogDataService.ListProductsAsync(searchInput);

        // Chuyển đổi dữ liệu ra View thông qua ViewBag
        ViewBag.Categories = filterCategories;
        ViewBag.ProductResult = productResult;
        ViewBag.CurrentCategoryID = categoryID;
        ViewBag.CurrentSearchValue = searchValue;
        ViewBag.CurrentMinPrice = minPrice;
        ViewBag.CurrentMaxPrice = maxPrice;

        return View();
    }

    /// <summary>
    /// Trả về tập hợp sản phẩm dựa trên tham số lọc (Sử dụng AJAX).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> FilterProducts(ProductSearchInput criteria)
    {
        // Xử lý logic validation cho mức giá: giá kết thúc (max) không được thấp hơn giá bắt đầu (min)
        if (criteria.MaxPrice > 0 && criteria.MinPrice > 0 && criteria.MaxPrice < criteria.MinPrice)
        {
            criteria.MaxPrice = 0;
        }

        // Đảm bảo các chỉ số lọc về giá là không âm
        criteria.MinPrice = Math.Max(0, criteria.MinPrice);
        criteria.MaxPrice = Math.Max(0, criteria.MaxPrice);

        criteria.PageSize = DEFAULT_ITEM_COUNT;
        
        var response = await CatalogDataService.ListProductsAsync(criteria);
        
        return PartialView("_ProductGrid", response);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return View(new ErrorViewModel { RequestId = requestId });
    }
}
