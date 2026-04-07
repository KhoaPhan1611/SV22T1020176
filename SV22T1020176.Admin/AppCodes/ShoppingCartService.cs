using SV22T1020176.Models.Sales;

namespace SV22T1020176.Admin;

/// <summary>
/// Dịch vụ quản lý các thao tác tương tác với giỏ hàng được lưu trữ trong Session.
/// </summary>
public static class ShoppingCartService
{
    private const string SESSION_CART_KEY = "ShoppingCart";

    /// <summary>
    /// Truy xuất toàn bộ danh sách mặt hàng hiện có trong giỏ hàng từ Session.
    /// </summary>
    public static List<OrderDetailViewInfo> GetShoppingCart()
    {
        var currentBasket = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(SESSION_CART_KEY);
        if (currentBasket is null)
        {
            currentBasket = new List<OrderDetailViewInfo>();
            ApplicationContext.SetSessionData(SESSION_CART_KEY, currentBasket);
        }
        return currentBasket;
    }

    /// <summary>
    /// Tìm kiếm một mặt hàng cụ thể trong giỏ dựa trên mã sản phẩm.
    /// </summary>
    public static OrderDetailViewInfo? GetCartItem(int productId) => GetShoppingCart().FirstOrDefault(x => x.ProductID == productId);

    /// <summary>
    /// Thêm một mục hàng mới hoặc cập nhật thông tin nếu mặt hàng đã tồn tại trong giỏ.
    /// </summary>
    public static void AddCartItem(OrderDetailViewInfo entry)
    {
        var basket = GetShoppingCart();
        var existing = basket.Find(i => i.ProductID == entry.ProductID);
        
        if (existing == null)
        {
            basket.Add(entry);
        }
        else
        {
            existing.Quantity += entry.Quantity;
            existing.SalePrice = entry.SalePrice;
        }
        
        SyncBasket(basket);
    }

    /// <summary>
    /// Điều chỉnh số lượng và đơn giá của một sản phẩm trong giỏ hàng.
    /// </summary>
    public static void UpdateCartItem(int productId, int newQty, decimal price)
    {
        var basket = GetShoppingCart();
        var target = basket.Find(i => i.ProductID == productId);
        if (target != null)
        {
            target.Quantity = newQty;
            target.SalePrice = price;
            SyncBasket(basket);
        }
    }

    /// <summary>
    /// Loại bỏ hoàn toàn một mặt hàng khỏi giỏ hàng.
    /// </summary>
    public static void RemoveCartItem(int productId)
    {
        var basket = GetShoppingCart();
        var match = basket.Find(i => i.ProductID == productId);
        if (match != null)
        {
            basket.Remove(match);
            SyncBasket(basket);
        }
    }

    /// <summary>
    /// Xóa bỏ toàn bộ dữ liệu giỏ hàng trong phiên làm việc hiện tại.
    /// </summary>
    public static void ClearCart() => ApplicationContext.SetSessionData(SESSION_CART_KEY, new List<OrderDetailViewInfo>());

    /// <summary>
    /// Đồng bộ hóa dữ liệu giỏ hàng vào Session.
    /// </summary>
    private static void SyncBasket(List<OrderDetailViewInfo> data) => ApplicationContext.SetSessionData(SESSION_CART_KEY, data);
}