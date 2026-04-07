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
    /// Lấy ID người dùng được xác thực hiện tại, xây dựng bộ tiêu chí tìm kiếm bao gồm trạng thái
    /// đơn hàng (mới, đã duyệt, đang giao, hoàn thành...), từ khóa tìm kiếm, và số trang. Truy vấn danh
    /// sách đơn hàng từ cơ sở dữ liệu, sau đó với mỗi đơn hàng, lấy danh sách chi tiết sản phẩm tương ứng
    /// để hiển thị thông tin sản phẩm từng đơn trực tiếp trên trang xem lịch sử mà không cần click vào chi tiết.
    /// </summary>
    public async Task<IActionResult> ExploreHistory(int status = 0, int page = 1, string searchValue = "")
    {
        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(currentUserIdStr, out int userId)) 
            return RedirectToAction("Authenticate", "Account");

        var searchCriteria = new OrderSearchInput
        {
            Page = Math.Max(1, page),
            PageSize = DEFAULT_PAGE_LIMIT,
            Status = (OrderStatusEnum)status,
            SearchValue = searchValue?.Trim() ?? string.Empty,
            CustomerID = userId
        };

        var orderHistory = await SalesDataService.ListOrdersAsync(searchCriteria).ConfigureAwait(false);
        ViewBag.Status = status;
        ViewBag.SearchValue = searchValue;

        var detailsByOrder = new Dictionary<int, List<OrderDetailViewInfo>>();
        foreach (var order in orderHistory.DataItems)
        {
            var details = await SalesDataService.ListDetailsAsync(order.OrderID).ConfigureAwait(false);
            detailsByOrder[order.OrderID] = details;
        }
        ViewBag.OrderDetails = detailsByOrder;

        return View("History", orderHistory);
    }

    /// <summary>
    /// Lấy ID đơn hàng từ URL, xác minh rằng đơn hàng thuộc về người dùng được xác thực hiện tại
    /// (kiểm tra CustomerID khớp). Nếu hợp lệ, truy vấn thông tin chi tiết đơn hàng bao gồm
    /// trạng thái hiện tại, thời gian đặt, thông tin giao hàng, hãng vận chuyển; và danh sách
    /// tất cả sản phẩm trong đơn hàng. Hiển thị trên giao diện chi tiết để khách hàng theo dõi
    /// tiến trình giao hàng và xem các mặt hàng đã mua.
    /// </summary>
    public async Task<IActionResult> TrackOrder(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int cid)) 
            return RedirectToAction("Authenticate", "Account");

        var orderData = await SalesDataService.GetOrderAsync(id).ConfigureAwait(false);
        if (orderData is null || orderData.CustomerID != cid)
            return RedirectToAction(nameof(ExploreHistory));

        var lineItems = await SalesDataService.ListDetailsAsync(id).ConfigureAwait(false);
        ViewBag.Details = lineItems;
        
        return View("Status", orderData);
    }

    /// <summary>
    /// Cho phép khách hàng được xác thực hủy đơn hàng nếu và chỉ nếu đơn hàng đang ở trạng thái
    /// "Mới" hoặc "Đã Duyệt" (chưa bắt đầu giao hàng). Phương thức kiểm tra quyền sở hữu đơn hàng,
    /// kiểm tra trạng thái cho phép hủy, gọi dịch vụ để cập nhật trạng thái đơn hàng thành "Đã Hủy",
    /// và thông báo kết quả cho khách hàng qua TempData.
    /// </summary>
    public async Task<IActionResult> TerminateOrder(int id)
    {
        var subjectId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(subjectId, out int memberId)) 
            return RedirectToAction("Authenticate", "Account");

        var orderRecord = await SalesDataService.GetOrderAsync(id).ConfigureAwait(false);
        if (orderRecord is null || orderRecord.CustomerID != memberId)
            return RedirectToAction(nameof(ExploreHistory));

        bool canCancel = orderRecord.Status is OrderStatusEnum.New or OrderStatusEnum.Accepted;
        if (!canCancel)
        {
            TempData["ErrorMessage"] = "Yêu cầu hủy không thành công: Đơn hàng đang được vận chuyển.";
            return RedirectToAction(nameof(TrackOrder), new { id });
        }

        bool cancelSuccess = await SalesDataService.CancelOrderAsync(id).ConfigureAwait(false);
        
        TempData[cancelSuccess ? "SuccessMessage" : "ErrorMessage"] = 
            cancelSuccess 
                ? "Đã tiếp nhận yêu cầu hủy đơn hàng thành công." 
                : "Hệ thống gặp sự cố khi xử lý lệnh hủy. Thử lại sau.";

        return RedirectToAction(nameof(ExploreHistory));
    }

    /// <summary>
    /// Cho phép khách hàng được xác thực tạo lại đơn hàng trước đó bằng cách sao chép tất cả sản phẩm
    /// từ đơn hàng cũ vào giỏ hàng hiện tại. Phương thức kiểm tra quyền sở hữu đơn hàng, truy vấn danh
    /// sách chi tiết sản phẩm trong đơn cũ, kiểm tra xem mỗi sản phẩm có còn được kinh doanh hay không,
    /// và thêm những sản phẩm khả dụng vào giỏ hàng (tăng số lượng nếu sản phẩm đã có). Thông báo số
    /// lượng sản phẩm đã thêm hoặc thất bại nếu không có sản phẩm nào khả dụng.
    /// </summary>
    public async Task<IActionResult> DuplicateOrder(int id)
    {
        var userClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userClaim, out int ownerId)) 
            return RedirectToAction("Authenticate", "Account");

        var sourceOrder = await SalesDataService.GetOrderAsync(id).ConfigureAwait(false);
        if (sourceOrder?.CustomerID != ownerId)
            return RedirectToAction(nameof(ExploreHistory));

        var orderLineItems = await SalesDataService.ListDetailsAsync(id).ConfigureAwait(false);
        if (orderLineItems.Count == 0)
        {
            TempData["ErrorMessage"] = "Không tìm thấy dữ liệu sản phẩm cho đơn hàng này.";
            return RedirectToAction(nameof(ExploreHistory));
        }

        var cartData = LoadCurrentUserCart();
        int addedCount = 0;

        foreach (var lineDetail in orderLineItems)
        {
            var productRecord = await CatalogDataService.GetProductAsync(lineDetail.ProductID).ConfigureAwait(false);
            
            if (productRecord?.IsSelling != true)
                continue;

            var existingInCart = cartData.FirstOrDefault(c => c.ProductID == lineDetail.ProductID);
            
            if (existingInCart != null)
            {
                existingInCart.Quantity += lineDetail.Quantity;
            }
            else
            {
                cartData.Add(new CartItem
                {
                    ProductID = productRecord.ProductID,
                    ProductName = productRecord.ProductName,
                    Photo = productRecord.Photo ?? string.Empty,
                    Price = productRecord.Price,
                    Unit = productRecord.Unit,
                    Quantity = lineDetail.Quantity
                });
            }
            
            addedCount++;
        }

        UpdateCartStorage(cartData);

        if (addedCount > 0)
        {
            TempData["SuccessMessage"] = $"Đã sao chép {addedCount} mặt hàng từ đơn #{id} vào giỏ hàng.";
        }
        else
        {
            TempData["ErrorMessage"] = "Toàn bộ sản phẩm trong đơn cũ hiện không còn kinh doanh.";
        }

        return RedirectToAction("Index", "Cart");
    }
}
