using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020176.BusinessLayers;
using SV22T1020176.Models.Sales;
using System.Security.Claims;
using SV22T1020176.Shop.Models;
using SV22T1020176.Models.Catalog;

namespace SV22T1020176.Shop.Controllers;

/// <summary>
/// Controller điều hướng việc truy vấn lịch sử đơn hàng và theo dõi trạng thái giao vận.
/// </summary>
[Authorize]
public class OrderController : Controller
{
    private const int DEFAULT_PAGE_LIMIT = 10;

    private List<CartItem> LoadCurrentUserCart() => CartSessionHelper.GetCart(HttpContext);
    private void UpdateCartStorage(List<CartItem> basket) => CartSessionHelper.SaveCart(HttpContext, basket);

    /// <summary>
    /// Hiển thị danh sách các đơn hàng người dùng đã thực hiện.
    /// </summary>
    public async Task<IActionResult> ExploreHistory(int status = 0, int page = 1, string searchValue = "")
    {
        var uidClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(uidClaim, out int accountId)) return RedirectToAction("Login", "Account");

        var filterInput = new OrderSearchInput
        {
            Page = page,
            PageSize = DEFAULT_PAGE_LIMIT,
            Status = (OrderStatusEnum)status,
            SearchValue = searchValue ?? string.Empty,
            CustomerID = accountId
        };

        var historyContent = await SalesDataService.ListOrdersAsync(filterInput);
        ViewBag.Status = status;
        ViewBag.SearchValue = searchValue;

        // Tập hợp chi tiết từng sản phẩm trong mỗi đơn hàng để hiển thị nhanh
        var itemsMapping = new Dictionary<int, List<OrderDetailViewInfo>>();
        foreach (var entry in historyContent.DataItems)
        {
            var detailEntries = await SalesDataService.ListDetailsAsync(entry.OrderID);
            itemsMapping[entry.OrderID] = detailEntries;
        }
        ViewBag.OrderDetails = itemsMapping;

        return View("History", historyContent);
    }

    /// <summary>
    /// Xem chi tiết và hành trình của một mã đơn h àng cụ thể.
    /// </summary>
    public async Task<IActionResult> TrackOrder(int id)
    {
        var uidStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(uidStr, out int cid)) return RedirectToAction("Login", "Account");

        var targetOrder = await SalesDataService.GetOrderAsync(id);
        if (targetOrder == null || targetOrder.CustomerID != cid)
        {
            return RedirectToAction(nameof(ExploreHistory));
        }

        ViewBag.Details = await SalesDataService.ListDetailsAsync(id);
        return View("Status", targetOrder);
    }

    /// <summary>
    /// Hủy bỏ yêu cầu đặt hàng (Chỉ áp dụng khi đơn hàng chưa được xử lý vận chuyển).
    /// </summary>
    public async Task<IActionResult> TerminateOrder(int id)
    {
        var authId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(authId, out int memberId)) return RedirectToAction("Login", "Account");

        var target = await SalesDataService.GetOrderAsync(id);
        if (target == null || target.CustomerID != memberId)
        {
            return RedirectToAction(nameof(ExploreHistory));
        }

        // Kiểm soát điều kiện hủy: Chỉ cho phép khi trạng thái là Mới hoặc Đã Duyệt
        bool canAbort = target.Status == OrderStatusEnum.New || target.Status == OrderStatusEnum.Accepted;
        if (!canAbort)
        {
            TempData["ErrorMessage"] = "Yêu cầu hủy không thành công: Đơn hàng đang được vận chuyển.";
            return RedirectToAction(nameof(TrackOrder), new { id });
        }

        bool result = await SalesDataService.CancelOrderAsync(id);
        if (result)
        {
            TempData["SuccessMessage"] = "Đã tiếp nhận yêu cầu hủy đơn hàng thành công.";
        }
        else
        {
            TempData["ErrorMessage"] = "Hệ thống gặp sự cố khi xử lý lệnh hủy. Thử lại sau.";
        }

        return RedirectToAction(nameof(ExploreHistory));
    }

    /// <summary>
    /// Mua lại các sản phẩm từ một đơn hàng cũ (Nạp lại vào giỏ hàng hiện tại).
    /// </summary>
    public async Task<IActionResult> DuplicateOrder(int id)
    {
        var userIdentity = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdentity, out int ownerId)) return RedirectToAction("Login", "Account");

        var sourceOrder = await SalesDataService.GetOrderAsync(id);
        if (sourceOrder == null || sourceOrder.CustomerID != ownerId)
        {
            return RedirectToAction(nameof(ExploreHistory));
        }

        var sourceDetails = await SalesDataService.ListDetailsAsync(id);
        if (!sourceDetails.Any())
        {
            TempData["ErrorMessage"] = "Không tìm thấy dữ liệu sản phẩm cho đơn hàng này.";
            return RedirectToAction(nameof(ExploreHistory));
        }

        var currentBasket = LoadCurrentUserCart();
        int successAdditions = 0;

        foreach (var detail in sourceDetails)
        {
            var freshProduct = await CatalogDataService.GetProductAsync(detail.ProductID);
            if (freshProduct != null && freshProduct.IsSelling == true)
            {
                var duplicateInCart = currentBasket.Find(c => c.ProductID == detail.ProductID);
                if (duplicateInCart != null)
                {
                    duplicateInCart.Quantity += detail.Quantity;
                }
                else
                {
                    currentBasket.Add(new CartItem
                    {
                        ProductID = freshProduct.ProductID,
                        ProductName = freshProduct.ProductName,
                        Photo = freshProduct.Photo ?? string.Empty,
                        Price = freshProduct.Price,
                        Unit = freshProduct.Unit,
                        Quantity = detail.Quantity
                    });
                }
                successAdditions++;
            }
        }

        UpdateCartStorage(currentBasket);

        if (successAdditions > 0)
        {
            TempData["SuccessMessage"] = $"Đã sao chép {successAdditions} mặt hàng từ đơn #{id} vào giỏ hàng.";
        }
        else
        {
            TempData["ErrorMessage"] = "Toàn bộ sản phẩm trong đơn cũ hiện không còn kinh doanh.";
        }

        return RedirectToAction("Index", "Cart");
    }
}
