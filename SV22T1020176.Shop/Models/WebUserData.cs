using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace SV22T1020176.Shop.Models
{
    /// <summary>
    /// Thông tin tài khoản khách hàng (Customer) được lưu trong cookie đăng nhập của Shop
    /// </summary>
    public class WebUserData
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? DisplayName { get; set; }
        public string? Photo { get; set; }
        public List<string>? Roles { get; set; }

        /// <summary>
        /// Tạo Principal dựa trên thông tin người dùng
        /// </summary>
        public ClaimsPrincipal CreatePrincipal()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, UserId ?? ""),
                new Claim(ClaimTypes.Name, UserName ?? ""),
                new Claim(nameof(DisplayName), DisplayName ?? ""),
                new Claim(nameof(Photo), Photo ?? "")
            };
            if (Roles != null)
                foreach (var role in Roles)
                    claims.Add(new Claim(ClaimTypes.Role, role));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }
    }
}
