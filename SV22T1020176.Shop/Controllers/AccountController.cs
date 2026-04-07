using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SV22T1020176.BusinessLayers;
using SV22T1020176.Models.Partner;
using SV22T1020176.Models.Security;
using SV22T1020176.Shop.Models;

namespace SV22T1020176.Shop.Controllers;

/// <summary>
/// Controller điều hướng các tính năng định danh: Đăng ký, Đăng nhập, Hồ sơ cá nhân và Bảo mật.
/// </summary>
public class AccountController : Controller
{
    /// <summary>
    /// Hiển thị giao diện khởi tạo tài khoản mới.
    /// </summary>
    [HttpGet]
    public IActionResult CreateAccount(string returnUrl = "")
    {
        ViewBag.ReturnUrl = returnUrl;
        return View("Register");
    }

    /// <summary>
    /// Xử lý logic đăng ký thành viên mới.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAccount(Customer registrationInfo, string confirmPassword, string returnUrl = "")
    {
        // Kiểm soát dữ liệu đầu vào: Tên và Email
        if (string.IsNullOrWhiteSpace(registrationInfo.CustomerName))
            ModelState.AddModelError(nameof(registrationInfo.CustomerName), "Vui lòng nhập họ tên đầy đủ.");
        else if (registrationInfo.CustomerName.Trim().Length < 2)
            ModelState.AddModelError(nameof(registrationInfo.CustomerName), "Danh tính phải chứa tối thiểu 2 ký tự.");

        if (string.IsNullOrWhiteSpace(registrationInfo.Email))
            ModelState.AddModelError(nameof(registrationInfo.Email), "Địa chỉ thư điện tử không được bỏ trống.");
        else if (!registrationInfo.Email.Contains('@') || !registrationInfo.Email.Contains('.'))
            ModelState.AddModelError(nameof(registrationInfo.Email), "Định dạng email không hợp lệ.");

        // Kiểm soát dữ liệu đầu vào: Liên lạc và Khu vực
        if (string.IsNullOrWhiteSpace(registrationInfo.Phone))
        {
            ModelState.AddModelError(nameof(registrationInfo.Phone), "Số điện thoại là thông tin bắt buộc.");
        }
        else
        {
            var cleanPhone = new string(registrationInfo.Phone.Where(char.IsDigit).ToArray());
            if (cleanPhone.Length < 10 || cleanPhone.Length > 11)
                ModelState.AddModelError(nameof(registrationInfo.Phone), "Độ dài số điện thoại không hợp lệ (10-11 số).");
        }

        if (string.IsNullOrWhiteSpace(registrationInfo.Province))
            ModelState.AddModelError(nameof(registrationInfo.Province), "Chưa xác định Tỉnh/Thành phố cư trú.");

        // Kiểm soát dữ liệu đầu vào: Bảo mật mật khẩu
        if (string.IsNullOrEmpty(registrationInfo.Password))
            ModelState.AddModelError(nameof(registrationInfo.Password), "Cần thiết lập mật khẩu truy cập.");
        else if (registrationInfo.Password.Length < 6)
            ModelState.AddModelError(nameof(registrationInfo.Password), "Mật khẩu quá ngắn (yêu cầu từ 6 ký tự).");

        if (registrationInfo.Password != confirmPassword)
            ModelState.AddModelError(string.Empty, "Mật khẩu xác nhận không trùng khớp với mật khẩu đã nhập.");

        // Kiểm tra tính độc nhất của email trong hệ thống
        if (!string.IsNullOrEmpty(registrationInfo.Email))
        {
            bool isAvailable = await PartnerDataService.ValidateCustomerEmailAsync(registrationInfo.Email);
            if (!isAvailable)
                ModelState.AddModelError(nameof(registrationInfo.Email), "Hệ thống ghi nhận email này đã tồn tại. Thử email khác.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View("Register", registrationInfo);
        }

        try
        {
            registrationInfo.ContactName = registrationInfo.CustomerName;
            registrationInfo.IsLocked = false;
            await PartnerDataService.AddCustomerAsync(registrationInfo);

            TempData["SuccessMessage"] = "Khởi tạo tài khoản thành công! Hãy thực hiện đăng nhập lần đầu.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return RedirectToAction(nameof(Authenticate), new { returnUrl });
            return RedirectToAction(nameof(Authenticate));
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Giao dịch không thành công do lỗi nền tảng. Vui lòng quay lại sau.");
            ViewBag.ReturnUrl = returnUrl;
            return View("Register", registrationInfo);
        }
    }

    /// <summary>
    /// Hiển thị giao diện đăng nhập hệ thống.
    /// </summary>
    [HttpGet]
    public IActionResult Authenticate(string returnUrl = "")
    {
        ViewBag.ReturnUrl = returnUrl;
        return View("Login");
    }

    /// <summary>
    /// Xác thực thông tin người dùng và thiết lập phiên làm việc.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Authenticate(string email, string password, bool rememberMe = false, string returnUrl = "")
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError(string.Empty, "Yêu cầu nhập đầy đủ chứng chỉ đăng nhập (Email & Mật khẩu).");
            return View("Login");
        }

        var authorizedMember = await SecurityDataService.AuthorizeAsync(email, password, UserTypes.Customer);
        if (authorizedMember == null)
        {
            ModelState.AddModelError(string.Empty, "Chứng chỉ không đúng hoặc tài khoản không tồn tại.");
            ViewData["Email"] = email;
            return View("Login");
        }

        // Kiểm tra tình trạng hoạt động của tài khoản
        var memberProfile = await PartnerDataService.GetCustomerAsync(int.Parse(authorizedMember.UserID));
        if (memberProfile?.IsLocked == true)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản hiện đang bị đình chỉ quyền truy cập.");
            ViewData["Email"] = email;
            return View("Login");
        }

        // Khởi tạo cookie danh tính (Identity Cookie)
        var identityData = new WebUserData
        {
            UserId = authorizedMember.UserID,
            UserName = authorizedMember.Email,
            DisplayName = authorizedMember.FullName,
            Photo = authorizedMember.Photo,
            Roles = new List<string> { "customer" }
        };

        var signinSettings = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(24)
        };

        await HttpContext.SignInAsync(identityData.CreatePrincipal(), signinSettings);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Truy xuất và hiển thị thông tin hồ sơ cá nhân của thành viên.
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MyProfile()
    {
        var customerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(customerIdStr, out int currentId)) return RedirectToAction(nameof(Authenticate));

        var data = await PartnerDataService.GetCustomerAsync(currentId);
        if (data == null) return RedirectToAction(nameof(Authenticate));

        return View("Profile", data);
    }

    /// <summary>
    /// Xử lý cập nhật thông tin định danh của thành viên.
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ReviseProfile(Customer inputModel)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(uid, out int accountId))
            return Json(new { success = false, message = "Lỗi xác thực: Vui lòng kết nối lại hệ thống." });

        var member = await PartnerDataService.GetCustomerAsync(accountId);
        if (member == null) return Json(new { success = false, message = "Thông tin không khả dụng." });

        // Kiểm soát cú pháp tên người dùng
        if (string.IsNullOrWhiteSpace(inputModel.CustomerName))
            return Json(new { success = false, message = "Họ tên là trường thông tin thiết yếu.", field = nameof(inputModel.CustomerName) });
        
        var namingPattern = new System.Text.RegularExpressions.Regex(@"^[\p{L} ]+$");
        if (!namingPattern.IsMatch(inputModel.CustomerName))
            return Json(new { success = false, message = "Họ tên chỉ cho phép ký tự chữ cái.", field = nameof(inputModel.CustomerName) });

        if (inputModel.CustomerName.Trim().Length < 2)
            return Json(new { success = false, message = "Họ tên quá ngắn.", field = nameof(inputModel.CustomerName) });

        if (!string.IsNullOrEmpty(inputModel.Phone))
        {
            var rawDigits = new string(inputModel.Phone.Where(char.IsDigit).ToArray());
            if (rawDigits.Length < 10 || rawDigits.Length > 11 || rawDigits.Length != inputModel.Phone.Length)
                return Json(new { success = false, message = "Định dạng số điện thoại chưa chuẩn xác.", field = nameof(inputModel.Phone) });
        }

        member.CustomerName = inputModel.CustomerName.Trim();
        member.ContactName = inputModel.CustomerName.Trim();
        member.Phone = inputModel.Phone;
        member.Province = inputModel.Province;
        member.Address = inputModel.Address;

        bool updated = await PartnerDataService.UpdateCustomerAsync(member);
        if (updated)
        {
            var newIdentity = new WebUserData
            {
                UserId = member.CustomerID.ToString(),
                UserName = member.Email,
                DisplayName = member.CustomerName,
                Photo = User.FindFirst("Photo")?.Value ?? string.Empty,
                Roles = new List<string> { "customer" }
            };
            await HttpContext.SignInAsync(newIdentity.CreatePrincipal());
            return Json(new { success = true, message = "Thông tin của bạn đã được cập nhật thành công!" });
        }

        return Json(new { success = false, message = "Không thể ghi nhận thay đổi lúc này." });
    }

    /// <summary>
    /// Thực hiện thay đổi mật khẩu đăng nhập của người dùng.
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ResetPassphrase(string curPassword, string newPassword, string confirmNewPassword)
    {
        if (string.IsNullOrEmpty(curPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmNewPassword))
            return Json(new { success = false, message = "Vui lòng cung cấp đầy đủ các chuỗi bảo mật." });

        if (newPassword != confirmNewPassword)
            return Json(new { success = false, message = "Mật khẩu xác nhận mới không trùng khớp.", field = "confirmNewPassword" });

        if (newPassword.Length < 6)
            return Json(new { success = false, message = "Mật khẩu mới yêu cầu độ phức tạp cao hơn (>=6 ký tự).", field = "newPassword" });

        var memberEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(memberEmail)) return Json(new { success = false, message = "Phiên làm việc hết hạn." });

        var authCheck = await SecurityDataService.AuthorizeAsync(memberEmail, curPassword, UserTypes.Customer);
        if (authCheck == null)
            return Json(new { success = false, message = "Mật khẩu cũ không đúng.", field = "curPassword" });

        bool isReset = await SecurityDataService.ChangePasswordAsync(memberEmail, newPassword, UserTypes.Customer);
        return Json(new { 
            success = isReset, 
            message = isReset ? "Mật khẩu của bạn đã được đổi mới thành công!" : "Xảy ra lỗi khi tái thiết lập mật khẩu." 
        });
    }

    /// <summary>
    /// Thực hiện việc hủy phiên làm việc và đăng xuất.
    /// </summary>
    public async Task<IActionResult> SignOff()
    {
        CartSessionHelper.ClearCart(HttpContext);
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
