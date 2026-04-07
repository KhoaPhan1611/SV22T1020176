using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020176.BusinessLayers;
using SV22T1020176.Models.Sales;
using SV22T1020176.Models.Common;
using SV22T1020176.Models.Catalog;
using SV22T1020176.Shop.Models;
using System.Security.Claims;

namespace SV22T1020176.Shop.Controllers;

/// <summary>
/// Controller điều khiển việc quản lý giỏ hàng, thanh toán và xử lý đơn hàng.
/// </summary>
public class CartController : Controller
{
    private List<CartItem> LoadBasket() => CartSessionHelper.GetCart(HttpContext);
    private void PersistBasket(List<CartItem> basket) => CartSessionHelper.SaveCart(HttpContext, basket);
    private int CountBasketEntries() => CartSessionHelper.GetCartCount(HttpContext);

    private List<CartItem> FilterSelectedEntries()
    {
        var currentBasket = LoadBasket();
        if (Request.Cookies.TryGetValue("selectedCartItems", out string? selectionIds) && !string.IsNullOrWhiteSpace(selectionIds))
        {
            var idSet = new HashSet<int>();
            foreach (var part in selectionIds.Split(','))
            {
                if (int.TryParse(part.Trim(), out int val)) idSet.Add(val);
            }
            
            if (idSet.Any())
            {
                return currentBasket.Where(entry => idSet.Contains(entry.ProductID)).ToList();
            }
        }
        return currentBasket;
    }

    /// <summary>
    /// Hiển thị danh sách các mặt hàng trong giỏ.
    /// </summary>
    public IActionResult Index() => View(LoadBasket());

    /// <summary>
    /// Thêm một mặt hàng mới vào giỏ hàng của người dùng.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AppendProduct(int productId, int count = 1)
    {
        // Xác thực người dùng trước khi cho phép thao tác giỏ hàng
        if (User?.Identity?.IsAuthenticated != true)
        {
            return Json(new
            {
                success = false,
                requireLogin = true,
                redirectUrl = Url.Action("Login", "Account", new { returnUrl = "/Cart" }),
                message = "Hệ thống yêu cầu đăng nhập để quản lý giỏ hàng."
            });
        }

        count = Math.Max(1, count); // Quy định số lượng tối thiểu là 1

        var basket = LoadBasket();
        var match = basket.Find(x => x.ProductID == productId);

        if (match != null)
        {
            match.Quantity += count;
        }
        else
        {
            var itemInfo = await CatalogDataService.GetProductAsync(productId);
            if (itemInfo == null)
            {
                return Json(new { success = false, message = "Thông tin sản phẩm không khả dụng." });
            }
            if (!itemInfo.IsSelling)
            {
                return Json(new { success = false, message = "Mặt hàng này hiện tại không còn kinh doanh." });
            }

            basket.Add(new CartItem
            {
                ProductID = itemInfo.ProductID,
                ProductName = itemInfo.ProductName,
                Photo = itemInfo.Photo ?? string.Empty,
                Price = itemInfo.Price,
                Unit = itemInfo.Unit,
                Quantity = count
            });
        }

        PersistBasket(basket);
        return Json(new { 
            success = true, 
            itemCount = CountBasketEntries(), 
            message = $"Thành công! Đã thêm \"{basket.Last().ProductName}\" vào giỏ." 
        });
    }

    /// <summary>
    /// Cập nhật số lượng của một mặt hàng cụ thể trong giỏ.
    /// </summary>
    [HttpPost]
    public IActionResult ModifyQuantity(int productId, int count)
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return Json(new { success = false, requireLogin = true, message = "Hết phiên đăng nhập." });
        }

        count = Math.Max(1, count);

        var basket = LoadBasket();
        var targetEntry = basket.FirstOrDefault(x => x.ProductID == productId);
        
        if (targetEntry != null)
        {
            targetEntry.Quantity = count;
            PersistBasket(basket);
            return Json(new
            {
                success = true,
                subtotal = targetEntry.TotalPrice.ToString("N0"),
                total = basket.Sum(x => x.TotalPrice).ToString("N0")
            });
        }
        return Json(new { success = false });
    }

    /// <summary>
    /// Loại bỏ một mặt hàng ra khỏi danh sách giỏ hàng.
    /// </summary>
    [HttpPost]
    public IActionResult DeleteEntry(int productId)
    {
        if (User?.Identity?.IsAuthenticated != true) return Json(new { success = false });

        var basket = LoadBasket();
        var entryToRemove = basket.Find(x => x.ProductID == productId);
        
        if (entryToRemove != null)
        {
            basket.Remove(entryToRemove);
            PersistBasket(basket);
            return Json(new
            {
                success = true,
                total = basket.Sum(x => x.TotalPrice).ToString("N0"),
                itemCount = basket.Count
            });
        }
        return Json(new { success = false });
    }

    /// <summary>
    /// Xóa toàn bộ nội dung giỏ hàng hiện tại.
    /// </summary>
    [HttpPost]
    public IActionResult EmptyBasket()
    {
        CartSessionHelper.ClearCart(HttpContext);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Chuyển tới giao diện điền thông tin giao hàng và xác nhận thanh toán.
    /// </summary>
    [Authorize]
    public async Task<IActionResult> Checkout()
    {
        var selectedItems = FilterSelectedEntries();
        if (!selectedItems.Any())
        {
            TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một sản phẩm để tiến hành đặt hàng.";
            return RedirectToAction(nameof(Index));
        }

        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(accountId, out int parsedId)) return RedirectToAction("Login", "Account");

        var profile = await PartnerDataService.GetCustomerAsync(parsedId);
        ViewBag.Cart = selectedItems;
        ViewBag.Shippers = await PartnerDataService.ListShippersAsync(new PaginationSearchInput { PageSize = 100 });

        return View(profile);
    }

    /// <summary>
    /// Hoàn tất quy trình đặt hàng và lưu vào cơ sở dữ liệu.
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ProcessOrder(
        string recipientName,
        string recipientPhone,
        string deliveryAddress,
        string deliveryProvince,
        int? shipperID,
        string note = "")
    {
        var purchaseItems = FilterSelectedEntries();
        if (!purchaseItems.Any()) return RedirectToAction(nameof(Index));

        var customerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(customerIdStr, out int customerId)) return RedirectToAction("Login", "Account");

        // Ràng buộc dữ liệu bắt buộc cho việc giao vận
        if (string.IsNullOrWhiteSpace(recipientName)) ModelState.AddModelError(nameof(recipientName), "Họ tên người nhận là bắt buộc.");
        if (string.IsNullOrWhiteSpace(recipientPhone)) ModelState.AddModelError(nameof(recipientPhone), "Số điện thoại liên lạc không được để trống.");
        if (string.IsNullOrWhiteSpace(deliveryAddress)) ModelState.AddModelError(nameof(deliveryAddress), "Địa chỉ nhận hàng không hợp lệ.");
        if (string.IsNullOrWhiteSpace(deliveryProvince)) ModelState.AddModelError(nameof(deliveryProvince), "Chưa chọn khu vực tỉnh/thành phố.");
        if (!shipperID.HasValue || shipperID.Value <= 0) ModelState.AddModelError(nameof(shipperID), "Vui lòng lựa chọn đơn vị vận chuyển.");

        if (!ModelState.IsValid)
        {
            ViewBag.Cart = purchaseItems;
            ViewBag.Shippers = await PartnerDataService.ListShippersAsync(new PaginationSearchInput { PageSize = 100 });
            ViewData["ValidationErrors"] = ModelState.ToDictionary(
                k => k.Key,
                v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
            );
            return View("Checkout", await PartnerDataService.GetCustomerAsync(customerId));
        }

        // Lưu vết thông tin đơn hàng
        var shippingInfo = $"{recipientName} | {recipientPhone} | {deliveryAddress}";
        var newOrder = new Order
        {
            CustomerID = customerId,
            DeliveryAddress = shippingInfo,
            DeliveryProvince = deliveryProvince,
            ShipperID = shipperID,
            OrderTime = DateTime.Now,
            Status = OrderStatusEnum.New
        };

        int createdOrderId = await SalesDataService.AddOrderAsync(newOrder);

        // Đổ dữ liệu chi tiết các mặt hàng đã mua
        foreach (var item in purchaseItems)
        {
            await SalesDataService.AddDetailAsync(new OrderDetail
            {
                OrderID = createdOrderId,
                ProductID = item.ProductID,
                Quantity = item.Quantity,
                SalePrice = item.Price
            });
        }

        // Cập nhật lại giỏ hàng (loại bỏ các món đã được thanh toán)
        var totalBasket = LoadBasket();
        foreach (var item in purchaseItems)
        {
            var matchInBasket = totalBasket.FirstOrDefault(x => x.ProductID == item.ProductID);
            if (matchInBasket != null) totalBasket.Remove(matchInBasket);
        }
        PersistBasket(totalBasket);
        Response.Cookies.Delete("selectedCartItems");

        TempData["SuccessMessage"] = $"Giao dịch thành công! Đơn hàng #{createdOrderId} đang chờ hệ thống xử lý.";
        return RedirectToAction("TrackOrder", "Order", new { id = createdOrderId });
    }

    /// <summary>
    /// Đồng bộ số lượng mặt hàng thực tế với giao diện người dùng.
    /// </summary>
    public IActionResult FetchCartCount()
    {
        bool hasUser = User?.Identity?.IsAuthenticated == true;
        return Json(new { count = hasUser ? CountBasketEntries() : 0 });
    }
}
