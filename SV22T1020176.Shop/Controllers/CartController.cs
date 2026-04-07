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
    /// Truy xuất danh sách sản phẩm hiện đang được lưu trữ trong giỏ hàng của phiên làm việc hiện tại
    /// từ session storage và trình bày chúng trên giao diện người dùng. Danh sách này bao gồm tên sản phẩm,
    /// hình ảnh, giá bán, số lượng, đơn vị tính, và tổng giá của mỗi mặt hàng cùng tổng giỏ hàng.
    /// </summary>
    public IActionResult Index() => View(LoadBasket());

    /// <summary>
    /// Tiếp nhận yêu cầu thêm sản phẩm từ khách hàng được xác thực. Phương thức kiểm tra xem sản phẩm
    /// có tồn tại trong cơ sở dữ liệu và đang được kinh doanh hay không. Nếu sản phẩm đã tồn tại trong giỏ
    /// hiện tại, hệ thống tăng số lượng; nếu không, thêm mục mới vào giỏ. Sau cùng, persistance giỏ
    /// hàng cập nhật vào session và trả về JSON response với trạng thái thành công/thất bại.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AppendProduct(int productId, int count = 1)
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return Json(new
            {
                success = false,
                requireLogin = true,
                redirectUrl = Url.Action("Authenticate", "Account", new { returnUrl = "/Cart" }),
                message = "Hệ thống yêu cầu đăng nhập để quản lý giỏ hàng."
            });
        }

        var quantityRequested = Math.Clamp(count, 1, int.MaxValue);
        var cartSession = LoadBasket();
        var existingItem = cartSession.FirstOrDefault(e => e.ProductID == productId);

        if (existingItem != null)
        {
            existingItem.Quantity += quantityRequested;
        }
        else
        {
            var productInfo = await CatalogDataService.GetProductAsync(productId).ConfigureAwait(false);
            
            if (productInfo is null)
                return Json(new { success = false, message = "Thông tin sản phẩm không khả dụng." });

            if (!productInfo.IsSelling)
                return Json(new { success = false, message = "Mặt hàng này hiện tại không còn kinh doanh." });

            var newCartEntry = new CartItem
            {
                ProductID = productInfo.ProductID,
                ProductName = productInfo.ProductName,
                Photo = productInfo.Photo ?? string.Empty,
                Price = productInfo.Price,
                Unit = productInfo.Unit,
                Quantity = quantityRequested
            };
            
            cartSession.Add(newCartEntry);
        }

        PersistBasket(cartSession);
        return Json(new 
        { 
            success = true, 
            itemCount = CountBasketEntries(), 
            message = $"Thành công! Đã thêm \"{cartSession.FirstOrDefault(i => i.ProductID == productId)?.ProductName}\" vào giỏ." 
        });
    }

    /// <summary>
    /// Nhận yêu cầu thay đổi số lượng mua của một sản phẩm đã có trong giỏ hàng. Phương thức tìm kiếm
    /// mục sản phẩm theo ID, cập nhật trường number lượng với giá trị mới, và lưu lại toàn bộ giỏ
    /// vào session. Trả về JSON response chứa giá tiền từng mục (subtotal) và tổng giỏ hàng (total)
    /// để giao diện có thể cập nhật dynamically mà không cần tải lại trang.
    /// </summary>
    [HttpPost]
    public IActionResult ModifyQuantity(int productId, int count)
    {
        if (User?.Identity?.IsAuthenticated != true)
            return Json(new { success = false, requireLogin = true, message = "Hết phiên đăng nhập." });

        var adjustedCount = Math.Clamp(count, 1, int.MaxValue);
        var basketData = LoadBasket();
        var targetProduct = basketData.FirstOrDefault(x => x.ProductID == productId);
        
        if (targetProduct is null)
            return Json(new { success = false });

        targetProduct.Quantity = adjustedCount;
        PersistBasket(basketData);
        
        return Json(new
        {
            success = true,
            subtotal = targetProduct.TotalPrice.ToString("N0"),
            total = basketData.Sum(x => x.TotalPrice).ToString("N0")
        });
    }

    /// <summary>
    /// Xóa hoàn toàn một sản phẩm cụ thể khỏi giỏ hàng dựa trên ID sản phẩm. Phương thức tìm kiếm
    /// vị trí của mục sản phẩm trong danh sách, loại bỏ nó nếu tồn tại, và cập nhật giỏ
    /// về session storage. Gửi trả JSON response bao gồm tổng giỏ hàng cập nhật và số lượng
    /// mục còn lại để người dùng thấy sự thay đổi ngay tức thì.
    /// </summary>
    [HttpPost]
    public IActionResult DeleteEntry(int productId)
    {
        if (User?.Identity?.IsAuthenticated != true) 
            return Json(new { success = false });

        var basketData = LoadBasket();
        var itemToRemove = basketData.FirstOrDefault(x => x.ProductID == productId);
        
        if (itemToRemove is null)
            return Json(new { success = false });

        basketData.Remove(itemToRemove);
        PersistBasket(basketData);
        
        return Json(new
        {
            success = true,
            total = basketData.Sum(x => x.TotalPrice).ToString("N0"),
            itemCount = basketData.Count
        });
    }

    /// <summary>
    /// Làm trống hoàn toàn giỏ hàng của người dùng bằng cách xóa tất cả các mục được lưu trữ trong session.
    /// Hành động này thường được sử dụng khi người dùng muốn bắt đầu lại từ đầu hoặc hủy bỏ tất cả
    /// các sản phẩm đã chọn. Sau khi làm trống, hệ thống chuyển hướng người dùng về trang giỏ hàng
    /// để hiển thị trạng thái giỏ rỗng.
    /// </summary>
    [HttpPost]
    public IActionResult EmptyBasket()
    {
        CartSessionHelper.ClearCart(HttpContext);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Chỉ có người dùng được xác thực mới truy cập được phương thức này. Nó kiểm tra xem có ít nhất
    /// một mục sản phẩm được chọn trong giỏ hay không. Nếu có, lấy thông tin hồ sơ khách hàng từ cơ sở
    /// dữ liệu, danh sách các hãng vận chuyển khả dụng (shipper), và hiển thị biểu mẫu checkout để
    /// người dùng nhập/xác nhận thông tin giao hàng và chọn phương thức vận chuyển.
    /// </summary>
    [Authorize]
    public async Task<IActionResult> Checkout()
    {
        var toOrderItems = FilterSelectedEntries();
        if (toOrderItems.Count == 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một sản phẩm để tiến hành đặt hàng.";
            return RedirectToAction(nameof(Index));
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int buyerId)) 
            return RedirectToAction("Authenticate", "Account");

        var customerData = await PartnerDataService.GetCustomerAsync(buyerId).ConfigureAwait(false);
        var availableShippers = await PartnerDataService.ListShippersAsync(
            new PaginationSearchInput { PageSize = 100 }).ConfigureAwait(false);

        ViewBag.Cart = toOrderItems;
        ViewBag.Shippers = availableShippers;

        return View(customerData);
    }

    /// <summary>
    /// Tiếp nhận thông tin hoàn chỉnh từ biểu mẫu checkout bao gồm tên/điện thoại người nhận, địa chỉ
    /// giao hàng, tỉnh/thành phố, lựa chọn hãng vận chuyển. Xác thực tất cả dữ liệu bắt buộc đã được cung cấp
    /// đầy đủ. Tạo bản ghi đơn hàng mới trong cơ sở dữ liệu cùng với chi tiết từng sản phẩm tương ứng,
    /// cập nhật giỏ hàng bằng cách xóa các mục đã thanh toán, và chuyển hướng người dùng tới trang
    /// xem chi tiết/theo dõi đơn hàng vừa tạo.
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
        var itemsToCheckout = FilterSelectedEntries();
        if (!itemsToCheckout.Any()) 
            return RedirectToAction(nameof(Index));

        var customerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(customerIdStr, out int cstmrId)) 
            return RedirectToAction("Authenticate", "Account");

        Func<bool> ValidateDeliveryInfo = () =>
        {
            if (string.IsNullOrWhiteSpace(recipientName)) return false;
            if (string.IsNullOrWhiteSpace(recipientPhone)) return false;
            if (string.IsNullOrWhiteSpace(deliveryAddress)) return false;
            if (string.IsNullOrWhiteSpace(deliveryProvince)) return false;
            if (!shipperID.HasValue || shipperID.Value <= 0) return false;
            return true;
        };

        if (!ValidateDeliveryInfo())
        {
            if (string.IsNullOrWhiteSpace(recipientName)) 
                ModelState.AddModelError(nameof(recipientName), "Họ tên người nhận là bắt buộc.");
            if (string.IsNullOrWhiteSpace(recipientPhone)) 
                ModelState.AddModelError(nameof(recipientPhone), "Số điện thoại liên lạc không được để trống.");
            if (string.IsNullOrWhiteSpace(deliveryAddress)) 
                ModelState.AddModelError(nameof(deliveryAddress), "Địa chỉ nhận hàng không hợp lệ.");
            if (string.IsNullOrWhiteSpace(deliveryProvince)) 
                ModelState.AddModelError(nameof(deliveryProvince), "Chưa chọn khu vực tỉnh/thành phố.");
            if (!shipperID.HasValue || shipperID.Value <= 0) 
                ModelState.AddModelError(nameof(shipperID), "Vui lòng lựa chọn đơn vị vận chuyển.");

            ViewBag.Cart = itemsToCheckout;
            ViewBag.Shippers = await PartnerDataService.ListShippersAsync(new PaginationSearchInput { PageSize = 100 }).ConfigureAwait(false);
            ViewData["ValidationErrors"] = ModelState.ToDictionary(
                k => k.Key,
                v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
            );
            return View("Checkout", await PartnerDataService.GetCustomerAsync(cstmrId).ConfigureAwait(false));
        }

        var composedDeliveryInfo = $"{recipientName} | {recipientPhone} | {deliveryAddress}";
        var orderRecord = new Order
        {
            CustomerID = cstmrId,
            DeliveryAddress = composedDeliveryInfo,
            DeliveryProvince = deliveryProvince,
            ShipperID = shipperID,
            OrderTime = DateTime.Now,
            Status = OrderStatusEnum.New
        };

        int newOrderId = await SalesDataService.AddOrderAsync(orderRecord).ConfigureAwait(false);

        foreach (var lineItem in itemsToCheckout)
        {
            await SalesDataService.AddDetailAsync(new OrderDetail
            {
                OrderID = newOrderId,
                ProductID = lineItem.ProductID,
                Quantity = lineItem.Quantity,
                SalePrice = lineItem.Price
            }).ConfigureAwait(false);
        }

        var currentCartItems = LoadBasket();
        var remainingItems = currentCartItems.Where(c => !itemsToCheckout.Any(ch => ch.ProductID == c.ProductID)).ToList();
        PersistBasket(remainingItems);
        Response.Cookies.Delete("selectedCartItems");

        TempData["SuccessMessage"] = $"Giao dịch thành công! Đơn hàng #{newOrderId} đang chờ hệ thống xử lý.";
        return RedirectToAction("TrackOrder", "Order", new { id = newOrderId });
    }

    /// <summary>
    /// Trả về số lượng mục hiện đang có trong giỏ hàng dưới dạng JSON response. Phương thức này 
    /// được gọi qua AJAX từ giao diện người dùng để cập nhật dynamically số lượng trong biểu tượng
    /// giỏ hàng ở header/navbar. Nếu người dùng chưa xác thực, số lượng trả về là 0.
    /// </summary>
    public IActionResult FetchCartCount()
    {
        bool hasUser = User?.Identity?.IsAuthenticated == true;
        return Json(new { count = hasUser ? CountBasketEntries() : 0 });
    }
}
