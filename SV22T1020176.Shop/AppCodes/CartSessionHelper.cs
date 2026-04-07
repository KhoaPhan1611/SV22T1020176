using SV22T1020176.Shop.Models;
using System.Text.Json;

namespace SV22T1020176.Shop
{
    /// <summary>
    /// Helper quản lý giỏ hàng trong Session
    /// Dùng chung cho CartController và OrderController
    /// </summary>
    public static class CartSessionHelper
    {
        public const string CART_SESSION_KEY = "UserCart";

        public static List<CartItem> GetCart(HttpContext httpContext)
        {
            var cartJson = httpContext.Session.GetString(CART_SESSION_KEY);
            return string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        public static void SaveCart(HttpContext httpContext, List<CartItem> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            httpContext.Session.SetString(CART_SESSION_KEY, cartJson);
        }

        public static int GetCartCount(HttpContext httpContext)
        {
            return GetCart(httpContext).Count;
        }

        public static void ClearCart(HttpContext httpContext)
        {
            httpContext.Session.Remove(CART_SESSION_KEY);
        }
    }
}
