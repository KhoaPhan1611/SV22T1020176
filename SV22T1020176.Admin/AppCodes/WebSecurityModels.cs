using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020176.Admin
{
    /// <summary>
    /// Thông tin tài khoản người dùng được lưu trong phiên đăng nhập (cookie)
    /// </summary>
    public class WebUserData
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string? Photo { get; set; }        
        public List<string>? Roles { get; set; }

        private List<Claim> Claims
        {
            get
            {
                List<Claim> claims = new List<Claim>()
                {
                    new Claim(nameof(UserId), UserId ?? ""),
                    new Claim(nameof(UserName), UserName ?? ""),
                    new Claim(nameof(DisplayName), DisplayName ?? ""),
                    new Claim(nameof(Email), Email ?? ""),
                    new Claim(nameof(Photo), Photo ?? "")                    
                };
                if (Roles != null)
                    foreach (var role in Roles)
                        claims.Add(new Claim(ClaimTypes.Role, role));
                return claims;
            }
        }

        public ClaimsPrincipal CreatePrincipal()
        {
            var claimIdentity = new ClaimsIdentity(Claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimPrincipal = new ClaimsPrincipal(claimIdentity);
            return claimPrincipal;
        }
    }

    public class WebUserRoles
    {        
        public const string Administrator = "admin";  
        public const string DataManager = "datamanager";
        public const string Sales = "sales";
    }

    public static class WebUserExtensions
    {
        public static WebUserData? GetUserData(this ClaimsPrincipal principal)
        {
            try
            {
                if (principal == null || principal.Identity == null || !principal.Identity.IsAuthenticated)
                    return null;

                var userData = new WebUserData();
                userData.UserId = principal.FindFirstValue(nameof(userData.UserId));
                userData.UserName = principal.FindFirstValue(nameof(userData.UserName));
                userData.DisplayName = principal.FindFirstValue(nameof(userData.DisplayName));
                userData.Email = principal.FindFirstValue(nameof(userData.Email));
                userData.Photo = principal.FindFirstValue(nameof(userData.Photo));
                userData.Roles = new List<string>();
                foreach (var claim in principal.FindAll(ClaimTypes.Role)) userData.Roles.Add(claim.Value);
                return userData;
            }
            catch
            {
                return null;
            }
        }
    }
}

namespace SV22T1020176.Admin.AppCodes
{
    /// <summary>
    /// Custom requirements for dynamic permission-based authorization.
    /// User needs at least one matching permission (OR logic).
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public IReadOnlyList<string> RequiredPermissions { get; }
        public PermissionRequirement(params string[] permissions)
        {
            RequiredPermissions = permissions.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Handles the logic for permission-based authorization checks.
    /// </summary>
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            foreach (var perm in requirement.RequiredPermissions)
            {
                if (context.User.HasClaim("Permission", perm))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Dynamically provides authorization policies by parsing permission strings.
    /// Format: Permission_module1_action1_module2_action2
    /// </summary>
    public class DynamicPermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallback;
        public DynamicPermissionPolicyProvider(IOptions<AuthorizationOptions> options) => _fallback = new DefaultAuthorizationPolicyProvider(options);

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith("Permission_", StringComparison.OrdinalIgnoreCase))
            {
                var combinedKey = policyName["Permission_".Length..];
                var segments = combinedKey.Split('_');
                if (segments.Length % 2 == 0)
                {
                    var perms = new List<string>();
                    for (int i = 0; i < segments.Length; i += 2) perms.Add($"{segments[i]}:{segments[i + 1]}");
                    if (perms.Any())
                    {
                        var policy = new AuthorizationPolicyBuilder().AddRequirements(new PermissionRequirement(perms.ToArray())).Build();
                        return Task.FromResult<AuthorizationPolicy?>(policy);
                    }
                }
            }
            return _fallback.GetPolicyAsync(policyName);
        }
    }

    /// <summary>
    /// Attribute to enforce permission-based access on controllers/methods.
    /// </summary>
    public class AuthorizePermissionAttribute : AuthorizeAttribute
    {
        public AuthorizePermissionAttribute(params string[] permissions)
        {
            var key = string.Join("_", permissions.OrderBy(p => p)).Replace(":", "_");
            Policy = $"Permission_{key}";
        }
    }
}