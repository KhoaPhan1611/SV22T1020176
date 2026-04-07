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
    /// Chuẩn bị và hiển thị biểu mẫu đăng ký tài khoản khách hàng mới cho người dùng chưa có tài khoản.
    /// Phương thức HTTP GET này trả về giao diện HTML chứa các trường nhập liệu bắt buộc như 
    /// tên đầy đủ, email, số điện thoại, tỉnh/thành phố, và mật khẩu. URL quay lại (returnUrl)
    /// được lưu trữ để chuyển hướng người dùng đến trang mong muốn sau khi hoàn tất đăng ký.
    /// </summary>
    [HttpGet]
    public IActionResult CreateAccount(string returnUrl = "")
    {
        ViewBag.ReturnUrl = returnUrl;
        return View("Register");
    }

    /// <summary>
    /// Nhận dữ liệu biểu mẫu từ người dùng, thực hiện xác thực toàn diện bao gồm kiểm tra định dạng email,
    /// độ phức tạp mật khẩu, tính hợp lệ số điện thoại, và sự tồn tại duy nhất của địa chỉ email trong cơ sở dữ liệu.
    /// Nếu tất cả các điều kiện xác thực đều thỏa mãn, hệ thống tạo bản ghi khách hàng mới trong cơ sở dữ liệu
    /// và gửi hướng dẫn người dùng tới trang đăng nhập để hoàn tất quy trình xác thực tài khoản.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAccount(Customer registrationInfo, string confirmPassword, string returnUrl = "")
    {
        Func<bool> ValidateAccountData = () =>
        {
            // Xác thực tên người dùng
            if (string.IsNullOrWhiteSpace(registrationInfo.CustomerName))
            {
                ModelState.AddModelError(nameof(registrationInfo.CustomerName), "Vui lòng nhập họ tên đầy đủ.");
                return false;
            }
            if (registrationInfo.CustomerName.Trim().Length < 2)
            {
                ModelState.AddModelError(nameof(registrationInfo.CustomerName), "Danh tính phải chứa tối thiểu 2 ký tự.");
                return false;
            }

            // Xác thực email
            if (string.IsNullOrWhiteSpace(registrationInfo.Email))
            {
                ModelState.AddModelError(nameof(registrationInfo.Email), "Địa chỉ thư điện tử không được bỏ trống.");
                return false;
            }
            if (!registrationInfo.Email.Contains('@') || !registrationInfo.Email.Contains('.'))
            {
                ModelState.AddModelError(nameof(registrationInfo.Email), "Định dạng email không hợp lệ.");
                return false;
            }

            // Xác thực số điện thoại
            if (string.IsNullOrWhiteSpace(registrationInfo.Phone))
            {
                ModelState.AddModelError(nameof(registrationInfo.Phone), "Số điện thoại là thông tin bắt buộc.");
                return false;
            }
            var phoneDigits = new string(registrationInfo.Phone.Where(c => char.IsDigit(c)).ToArray());
            if (phoneDigits.Length is < 10 or > 11)
            {
                ModelState.AddModelError(nameof(registrationInfo.Phone), "Độ dài số điện thoại không hợp lệ (10-11 số).");
                return false;
            }

            // Xác thực tỉnh thành
            if (string.IsNullOrWhiteSpace(registrationInfo.Province))
            {
                ModelState.AddModelError(nameof(registrationInfo.Province), "Chưa xác định Tỉnh/Thành phố cư trú.");
                return false;
            }

            // Xác thực mật khẩu
            if (string.IsNullOrEmpty(registrationInfo.Password))
            {
                ModelState.AddModelError(nameof(registrationInfo.Password), "Cần thiết lập mật khẩu truy cập.");
                return false;
            }
            if (registrationInfo.Password.Length < 6)
            {
                ModelState.AddModelError(nameof(registrationInfo.Password), "Mật khẩu quá ngắn (yêu cầu từ 6 ký tự).");
                return false;
            }
            if (registrationInfo.Password != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu xác nhận không trùng khớp với mật khẩu đã nhập.");
                return false;
            }

            return true;
        };

        if (!ValidateAccountData())
        {
            ViewBag.ReturnUrl = returnUrl;
            return View("Register", registrationInfo);
        }

        bool emailExists = await PartnerDataService.ValidateCustomerEmailAsync(registrationInfo.Email).ConfigureAwait(false);
        if (!emailExists)
        {
            ModelState.AddModelError(nameof(registrationInfo.Email), "Hệ thống ghi nhận email này đã tồn tại. Thử email khác.");
            ViewBag.ReturnUrl = returnUrl;
            return View("Register", registrationInfo);
        }

        try
        {
            registrationInfo.ContactName = registrationInfo.CustomerName;
            registrationInfo.IsLocked = false;
            
            await PartnerDataService.AddCustomerAsync(registrationInfo).ConfigureAwait(false);
            TempData["SuccessMessage"] = "Khởi tạo tài khoản thành công! Hãy thực hiện đăng nhập lần đầu.";

            return Url.IsLocalUrl(returnUrl) && !string.IsNullOrEmpty(returnUrl) 
                ? RedirectToAction(nameof(Authenticate), new { returnUrl }) 
                : RedirectToAction(nameof(Authenticate));
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Giao dịch không thành công do lỗi nền tảng. Vui lòng quay lại sau.");
            ViewBag.ReturnUrl = returnUrl;
            return View("Register", registrationInfo);
        }
    }

    /// <summary>
    /// Cung cấp giao diện biểu mẫu đăng nhập cho người dùng và khách truy cập không xác thực.
    /// Trang này chứa các trường nhập liệu cho email/tên người dùng và mật khẩu, cùng với tùy chọn
    /// "ghi nhớ thông tin đăng nhập" để duy trì phiên làm việc kéo dài hơn. URL chuyển hướng
    /// được truyền qua URL để quay lại trang ban đầu sau khi xác thực thành công.
    /// </summary>
    [HttpGet]
    public IActionResult Authenticate(string returnUrl = "")
    {
        ViewBag.ReturnUrl = returnUrl;
        return View("Login");
    }

    /// <summary>
    /// Tiếp nhận thông tin xác thực từ người dùng bao gồm email/tên tài khoản và mật khẩu, gọi đến
    /// dịch vụ bảo mật để xác minh thông tin. Nếu xác thực thành công, phương thức kiểm tra trạng thái
    /// hoạt động của tài khoản (không bị khóa/vô hiệu hóa). Sau đó, thiết lập cookie xác thực và phiên
    /// làm việc với tùy chọn "ghi nhớ" để duy trì đăng nhập trong 30 ngày hoặc 24 giờ theo cài đặt.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Authenticate(string email, string password, bool rememberMe = false, string returnUrl = "")
    {
        if (string.IsNullOrEmpty(email?.Trim()) || string.IsNullOrEmpty(password?.Trim()))
        {
            ModelState.AddModelError(string.Empty, "Yêu cầu nhập đầy đủ chứng chỉ đăng nhập (Email & Mật khẩu).");
            return View("Login");
        }

        var credentialCheckResult = await SecurityDataService.AuthorizeAsync(email, password, UserTypes.Customer).ConfigureAwait(false);
        if (credentialCheckResult is null)
        {
            ModelState.AddModelError(string.Empty, "Chứng chỉ không đúng hoặc tài khoản không tồn tại.");
            ViewData["Email"] = email;
            return View("Login");
        }

        if (!int.TryParse(credentialCheckResult.UserID, out int customerId))
        {
            ModelState.AddModelError(string.Empty, "Lỗi hệ thống khi xác minh tài khoản.");
            return View("Login");
        }

        var profileData = await PartnerDataService.GetCustomerAsync(customerId).ConfigureAwait(false);
        if (profileData?.IsLocked ?? false)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản hiện đang bị đình chỉ quyền truy cập.");
            ViewData["Email"] = email;
            return View("Login");
        }

        var sessionIdentity = new WebUserData
        {
            UserId = credentialCheckResult.UserID,
            UserName = credentialCheckResult.Email,
            DisplayName = credentialCheckResult.FullName,
            Photo = credentialCheckResult.Photo,
            Roles = new List<string> { "customer" }
        };

        var authProps = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(rememberMe ? 720 : 24)
        };

        await HttpContext.SignInAsync(sessionIdentity.CreatePrincipal(), authProps).ConfigureAwait(false);

        if (Url.IsLocalUrl(returnUrl) && returnUrl.StartsWith("/"))
            return Redirect(returnUrl);
        
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Lấy ID người dùng từ phiên xác thực hiện tại, truy vấn cơ sở dữ liệu để lấy thông tin
    /// hồ sơ khách hàng đầy đủ bao gồm tên, email, số điện thoại, địa chỉ giao hàng mặc định, và tỉnh/
    /// thành phố cư trú. Sau đó hiển thị các thông tin này trên giao diện hồ sơ cá nhân để người dùng
    /// có thể xem lại các chi tiết cá nhân của mình. Yêu cầu người dùng phải đăng nhập để truy cập.
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MyProfile()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int currentId)) 
            return RedirectToAction(nameof(Authenticate));

        var profileData = await PartnerDataService.GetCustomerAsync(currentId).ConfigureAwait(false);
        if (profileData is null) 
            return RedirectToAction(nameof(Authenticate));

        return View("Profile", profileData);
    }

    /// <summary>
    /// Chấp nhận dữ liệu biểu mẫu cập nhật từ khách hàng được xác thực, thực hiện xác thực dữ liệu
    /// bao gồm kiểm tra tên chỉ chứa ký tự chữ cái, độ dài tên tối thiểu 2 ký tự, định dạng số điện thoại,
    /// và địa chỉ bắt buộc. Cập nhật các trường được phép vào cơ sở dữ liệu và làm mới cookie xác thực
    /// với thông tin cá nhân mới để phản ánh ngay lập tức trên giao diện người dùng.
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ReviseProfile(Customer inputModel)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int acctId))
            return Json(new { success = false, message = "Lỗi xác thực: Vui lòng kết nối lại hệ thống." });

        var existingMember = await PartnerDataService.GetCustomerAsync(acctId).ConfigureAwait(false);
        if (existingMember is null) 
            return Json(new { success = false, message = "Thông tin không khả dụng." });

        var nameRegex = new System.Text.RegularExpressions.Regex(@"^[\p{L}\s]+$");
        
        Func<bool> ValidateUpdates = () =>
        {
            if (string.IsNullOrWhiteSpace(inputModel.CustomerName))
                return false;
            if (inputModel.CustomerName.Trim().Length < 2)
                return false;
            if (!nameRegex.IsMatch(inputModel.CustomerName))
                return false;
            if (!string.IsNullOrEmpty(inputModel.Phone))
            {
                var justDigits = new string(inputModel.Phone.Where(c => char.IsDigit(c)).ToArray());
                if (justDigits.Length is < 10 or > 11 || justDigits.Length != inputModel.Phone.Length)
                    return false;
            }
            return true;
        };

        if (!ValidateUpdates())
        {
            return Json(new 
            { 
                success = false, 
                message = "Thông tin cập nhật không hợp lệ. Vui lòng kiểm tra lại.",
                field = nameof(inputModel.CustomerName)
            });
        }

        existingMember.CustomerName = inputModel.CustomerName?.Trim() ?? string.Empty;
        existingMember.ContactName = existingMember.CustomerName;
        existingMember.Phone = inputModel.Phone ?? string.Empty;
        existingMember.Province = inputModel.Province ?? string.Empty;
        existingMember.Address = inputModel.Address ?? string.Empty;

        bool updateSuccess = await PartnerDataService.UpdateCustomerAsync(existingMember).ConfigureAwait(false);
        
        if (!updateSuccess)
            return Json(new { success = false, message = "Không thể ghi nhận thay đổi lúc này." });

        var updatedIdentity = new WebUserData
        {
            UserId = existingMember.CustomerID.ToString(),
            UserName = existingMember.Email,
            DisplayName = existingMember.CustomerName,
            Photo = User.FindFirst("Photo")?.Value ?? string.Empty,
            Roles = new List<string> { "customer" }
        };
        
        await HttpContext.SignInAsync(updatedIdentity.CreatePrincipal()).ConfigureAwait(false);
        return Json(new { success = true, message = "Thông tin của bạn đã được cập nhật thành công!" });
    }

    /// <summary>
    /// Cho phép người dùng được xác thực thay đổi mật khẩu bằng cách nhập mật khẩu hiện tại để xác minh
    /// quyền sở hữu, mật khẩu mới mong muốn, và xác nhận mật khẩu mới. Phương thức xác thực mật khẩu cũ
    /// thông qua dịch vụ bảo mật, kiểm tra độ phức tạp mật khẩu mới (tối thiểu 6 ký tự), và đảm bảo hai
    /// trường mật khẩu mới khớp với nhau trước khi cập nhật vào cơ sở dữ liệu.
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ResetPassphrase(string curPassword, string newPassword, string confirmNewPassword)
    {
        var requiredFields = new[] { curPassword, newPassword, confirmNewPassword };
        if (requiredFields.Any(f => string.IsNullOrEmpty(f?.Trim())))
            return Json(new { success = false, message = "Vui lòng cung cấp đầy đủ các chuỗi bảo mật." });

        if (newPassword != confirmNewPassword)
            return Json(new 
            { 
                success = false, 
                message = "Mật khẩu xác nhận mới không trùng khớp.",
                field = "confirmNewPassword" 
            });

        if (newPassword.Length < 6)
            return Json(new 
            { 
                success = false, 
                message = "Mật khẩu mới yêu cầu độ phức tạp cao hơn (>=6 ký tự).",
                field = "newPassword" 
            });

        var principalEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(principalEmail))
            return Json(new { success = false, message = "Phiên làm việc hết hạn." });

        var passwordValidation = await SecurityDataService.AuthorizeAsync(principalEmail, curPassword, UserTypes.Customer).ConfigureAwait(false);
        if (passwordValidation is null)
            return Json(new 
            { 
                success = false, 
                message = "Mật khẩu cũ không đúng.",
                field = "curPassword" 
            });

        bool passwordUpdateResult = await SecurityDataService.ChangePasswordAsync(principalEmail, newPassword, UserTypes.Customer).ConfigureAwait(false);
        
        return Json(new 
        { 
            success = passwordUpdateResult, 
            message = passwordUpdateResult 
                ? "Mật khẩu của bạn đã được đổi mới thành công!" 
                : "Xảy ra lỗi khi tái thiết lập mật khẩu." 
        });
    }

    /// <summary>
    /// Kết thúc phiên làm việc của người dùng hiện tại bằng cách xóa toàn bộ dữ liệu giỏ hàng
    /// từ session, xóa cookie xác thực, và hủy quyền truy cập. Sau khi hoàn tất các bước đăng xuất,
    /// người dùng được chuyển hướng về trang chủ như một khách vô danh không xác thực.
    /// </summary>
    public async Task<IActionResult> SignOff()
    {
        CartSessionHelper.ClearCart(HttpContext);
        await HttpContext.SignOutAsync().ConfigureAwait(false);
        return RedirectToAction("Index", "Home");
    }
}
