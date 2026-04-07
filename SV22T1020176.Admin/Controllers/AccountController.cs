using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SV22T1020176.Admin;
using SV22T1020176.BusinessLayers;
using SV22T1020176.Models.Constants;
using SV22T1020176.Models.Security;
using System.Security.Claims;

namespace SV22T1020176.Admin.Controllers;

public class AccountController : Controller
{
    /// <summary>
    /// Giao diện đăng nhập
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToDefaultLanding();

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    /// <summary>
    /// Xử lý đăng nhập
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Vui lòng nhập email và mật khẩu.");
            return View();
        }

        var userAccount = await SecurityDataService.AuthorizeAsync(email, password, UserTypes.Employee);
        if (userAccount == null)
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            return View();
        }

        // Lấy thông tin chi tiết nhân viên
        var employee = await HRDataService.GetEmployeeAsync(
            int.TryParse(userAccount.UserID, out var empId) ? empId : 0
        );

        if (employee == null)
        {
            ModelState.AddModelError("", "Không tìm thấy thông tin chi tiết nhân viên.");
            return View();
        }

        // Lấy danh sách Roles và Permissions dựa trên Roles.cs
        var roles = new List<string>();
        var permissions = new List<string>();

        if (!string.IsNullOrWhiteSpace(employee.RoleNames))
        {
            var dbRoles = employee.RoleNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var dbRole in dbRoles)
            {
                // Tìm Role chuẩn trong Roles.cs (không phân biệt hoa thường)
                var standardizedRole = Roles.RolePermissions.Keys.FirstOrDefault(k => k.Equals(dbRole, StringComparison.OrdinalIgnoreCase));
                if (standardizedRole != null)
                {
                    roles.Add(standardizedRole);
                    var rolePerms = Roles.GetPermissions(standardizedRole);
                    foreach (var perm in rolePerms)
                    {
                        if (!permissions.Contains(perm))
                            permissions.Add(perm);
                    }
                }
            }
        }

        // Tạo Claims chuẩn
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, employee.EmployeeID.ToString()),
            new Claim(ClaimTypes.Email, employee.Email),
            new Claim(ClaimTypes.Name, employee.FullName),
            new Claim("Photo", employee.Photo ?? ""),
            new Claim("UserId", employee.EmployeeID.ToString()), // Hỗ trợ ApplicationContext.CurrentUser
            new Claim("UserName", employee.Email),               // Hỗ trợ ApplicationContext.CurrentUser
            new Claim("DisplayName", employee.FullName),         // Hỗ trợ ApplicationContext.CurrentUser
        };

        // Thêm Role Claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Thêm Permission Claims
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("Permission", permission));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            claimsPrincipal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            }
        );

        // Lưu session (Tùy chọn, vì ta dùng Claims là chính)
        HttpContext.Session.SetInt32("EmployeeID", employee.EmployeeID);
        HttpContext.Session.SetString("EmployeeEmail", employee.Email);
        HttpContext.Session.SetString("EmployeeName", employee.FullName);
        HttpContext.Session.SetString("EmployeeRoles", string.Join(",", roles));

        // Chuyển hướng
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectAfterLogin(permissions);
    }

    /// <summary>
    /// Shipper không có dashboard:view — không đẩy về / (403). Ưu tiên trang đơn hàng nếu có quyền.
    /// </summary>
    private IActionResult RedirectAfterLogin(List<string> permissions)
    {
        if (permissions.Contains(Permissions.DashboardView))
            return RedirectToAction("Index", "Home");
        if (permissions.Contains(Permissions.OrderView))
            return RedirectToAction("Index", "Order");
        return RedirectToAction("Profile", "Account");
    }

    /// <summary>
    /// Khi đã đăng nhập mà mở lại /Account/Login — điều hướng theo quyền (không ép shipper về /).
    /// </summary>
    private IActionResult RedirectToDefaultLanding()
    {
        if (User.HasClaim("Permission", Permissions.DashboardView))
            return RedirectToAction("Index", "Home");
        if (User.HasClaim("Permission", Permissions.OrderView))
            return RedirectToAction("Index", "Order");
        return RedirectToAction("Profile", "Account");
    }

    /// <summary>
    /// Đăng xuất
    /// </summary>
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    /// <summary>
    /// Giao diện từ chối truy cập
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    /// <summary>
    /// Giao diện hiển thị thông tin cá nhân
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var employeeId = HttpContext.Session.GetInt32("EmployeeID");
        if (employeeId == null)
            return RedirectToAction("Login");

        var employee = await HRDataService.GetEmployeeAsync(employeeId.Value);
        if (employee == null)
            return RedirectToAction("Login");

        return View(employee);
    }

    /// <summary>
    /// Cập nhật thông tin cá nhân
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Profile(
        string fullName, string email, string phone, string address,
        DateTime? birthDate, IFormFile? uploadPhoto)
    {
        var employeeId = HttpContext.Session.GetInt32("EmployeeID");
        if (employeeId == null)
            return RedirectToAction("Login");

        var employee = await HRDataService.GetEmployeeAsync(employeeId.Value);
        if (employee == null)
            return RedirectToAction("Login");

        if (string.IsNullOrWhiteSpace(fullName))
            ModelState.AddModelError(nameof(fullName), "Vui lòng nhập họ tên");
        if (string.IsNullOrWhiteSpace(email))
            ModelState.AddModelError(nameof(email), "Vui lòng nhập email");
        else if (!await HRDataService.ValidateEmployeeEmailAsync(email, employee.EmployeeID))
            ModelState.AddModelError(nameof(email), "Email đã được sử dụng bởi nhân viên khác");

        if (birthDate.HasValue && birthDate.Value.Date > DateTime.Today)
            ModelState.AddModelError(nameof(birthDate), "Ngày sinh không được là ngày trong tương lai");

        if (!ModelState.IsValid)
            return View(employee);

        // Xử lý upload ảnh
        if (uploadPhoto != null && uploadPhoto.Length > 0)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
            var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images", "employees", fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await uploadPhoto.CopyToAsync(stream);
            }
            employee.Photo = fileName;

            // Cập nhật lại claim Photo
            var identity = (ClaimsIdentity?)User.Identity;
            var photoClaim = identity?.FindFirst("Photo");
            if (photoClaim != null)
                identity?.RemoveClaim(photoClaim);
            identity?.AddClaim(new Claim("Photo", fileName));
        }

        employee.FullName = fullName.Trim();
        employee.Email = email.Trim();
        employee.Phone = phone?.Trim() ?? "";
        employee.Address = address?.Trim() ?? "";
        employee.BirthDate = birthDate;

        var result = await HRDataService.UpdateEmployeeAsync(employee);
        if (!result)
        {
            ModelState.AddModelError("", "Không thể cập nhật thông tin. Vui lòng thử lại.");
            return View(employee);
        }

        // Cập nhật session
        HttpContext.Session.SetString("EmployeeName", employee.FullName);
        HttpContext.Session.SetString("EmployeeEmail", employee.Email);

        // Cập nhật lại authentication cookie
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Standardize role names giống như Login action
        var standardizedRoles = new List<string>();
        var permissions = new List<string>();
        if (!string.IsNullOrWhiteSpace(employee.RoleNames))
        {
            var dbRoles = employee.RoleNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var dbRole in dbRoles)
            {
                var standardizedRole = Roles.RolePermissions.Keys.FirstOrDefault(k => k.Equals(dbRole, StringComparison.OrdinalIgnoreCase));
                if (standardizedRole != null && !standardizedRoles.Contains(standardizedRole))
                {
                    standardizedRoles.Add(standardizedRole);
                    var rolePerms = Roles.GetPermissions(standardizedRole);
                    foreach (var perm in rolePerms)
                    {
                        if (!permissions.Contains(perm))
                            permissions.Add(perm);
                    }
                }
            }
        }

        var newClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, employee.EmployeeID.ToString()),
            new Claim(ClaimTypes.Email, employee.Email),
            new Claim(ClaimTypes.Name, employee.FullName),
            new Claim("Photo", employee.Photo ?? ""),
            new Claim("UserId", employee.EmployeeID.ToString()),
            new Claim("UserName", employee.Email),
            new Claim("DisplayName", employee.FullName),
        };
        foreach (var role in standardizedRoles)
            newClaims.Add(new Claim(ClaimTypes.Role, role));
        foreach (var perm in permissions)
            newClaims.Add(new Claim("Permission", perm));

        var newIdentity = new ClaimsIdentity(newClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var newPrincipal = new ClaimsPrincipal(newIdentity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            newPrincipal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            }
        );

        TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công.";
        return RedirectToAction("Profile");
    }

    /// <summary>
    /// Giao diện thay đổi mật khẩu
    /// </summary>
    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        if (HttpContext.Session.GetInt32("EmployeeID") == null)
            return RedirectToAction("Login");

        return View();
    }

    /// <summary>
    /// Xử lý thay đổi mật khẩu
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
    {
        var employeeId = HttpContext.Session.GetInt32("EmployeeID");
        if (employeeId == null)
            return RedirectToAction("Login");

        if (string.IsNullOrWhiteSpace(oldPassword))
            ModelState.AddModelError(nameof(oldPassword), "Vui lòng nhập mật khẩu cũ");

        if (string.IsNullOrWhiteSpace(newPassword))
            ModelState.AddModelError(nameof(newPassword), "Vui lòng nhập mật khẩu mới");
        else if (newPassword.Length < 6)
            ModelState.AddModelError(nameof(newPassword), "Mật khẩu mới phải có ít nhất 6 ký tự");

        if (newPassword != confirmPassword)
            ModelState.AddModelError(nameof(confirmPassword), "Mật khẩu xác nhận không khớp");

        if (!ModelState.IsValid)
            return View();

        var employee = await HRDataService.GetEmployeeAsync(employeeId.Value);
        if (employee == null)
            return RedirectToAction("Login");

        var authResult = await SecurityDataService.AuthorizeAsync(employee.Email, oldPassword, UserTypes.Employee);
        if (authResult == null)
        {
            ModelState.AddModelError(nameof(oldPassword), "Mật khẩu cũ không đúng");
            return View();
        }

        var result = await HRDataService.ChangeEmployeePasswordAsync(employeeId.Value, newPassword);
        if (!result)
        {
            ModelState.AddModelError("", "Không thể đổi mật khẩu. Vui lòng thử lại.");
            return View();
        }

        TempData["SuccessMessage"] = "Đổi mật khẫu thành công.";
        return RedirectToAction("ChangePassword");
    }
}
