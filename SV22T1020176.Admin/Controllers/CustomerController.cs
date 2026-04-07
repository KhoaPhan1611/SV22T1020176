using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SV22T1020176.Admin;
using SV22T1020176.BusinessLayers;
using SV22T1020176.Models.Common;
using SV22T1020176.Models.Constants;
using SV22T1020176.Models.Partner;

namespace SV22T1020176.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý khách hàng (Customer).
    /// </summary>
    [Authorize]
    public class CustomerController : Controller
    {
        private const int PAGE_SIZE = 10;
        private const string CUSTOMER_SEARCH_CONDITION = "CustomerSearchCondition";

        /// <summary>
        /// Giao diện tìm kiếm và hiển thị danh sách khách hàng
        /// </summary>
        public IActionResult Index()
        {
            if (!ApplicationContext.HasPermission(Permissions.CustomerView))
                return RedirectToAction("AccessDenied", "Account");
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CUSTOMER_SEARCH_CONDITION);
            if (input == null)
            {
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = PAGE_SIZE,
                    SearchValue = ""
                };
            }
            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và hiển thị danh sách khách hàng (partial view)
        /// </summary>
        public async Task<IActionResult> Search(int page = 1, string searchValue = "")
        {
            if (!ApplicationContext.HasPermission(Permissions.CustomerView))
                return Forbid();
            var input = new PaginationSearchInput()
            {
                Page = page,
                PageSize = PAGE_SIZE,
                SearchValue = searchValue
            };
            ApplicationContext.SetSessionData(CUSTOMER_SEARCH_CONDITION, input);
            var data = await PartnerDataService.ListCustomersAsync(input);
            return PartialView(data);
        }

        /// <summary>
        /// Giao diện thêm mới khách hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!ApplicationContext.HasPermission(Permissions.CustomerCreate))
                return RedirectToAction("AccessDenied", "Account");
            ViewBag.Title = "Bổ sung khách hàng";
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            var model = new Customer() { CustomerID = 0, IsLocked = false };
            return View(model);
        }

        /// <summary>
        /// Xử lý thêm mới khách hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(Customer data)
        {
            if (!ApplicationContext.HasPermission(Permissions.CustomerCreate))
                return RedirectToAction("AccessDenied", "Account");
            if (string.IsNullOrWhiteSpace(data.CustomerName))
                ModelState.AddModelError(nameof(data.CustomerName), "Tên khách hàng không được để trống");
            if (string.IsNullOrWhiteSpace(data.ContactName))
                ModelState.AddModelError(nameof(data.ContactName), "Tên giao dịch không được để trống");
            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Email không được để trống");
            if (string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh thành");
            if (string.IsNullOrWhiteSpace(data.Address))
                ModelState.AddModelError(nameof(data.Address), "Địa chỉ không được để trống");
            if (string.IsNullOrWhiteSpace(data.Phone))
                ModelState.AddModelError(nameof(data.Phone), "Số điện thoại không được để trống");
            if (!string.IsNullOrWhiteSpace(data.Email) && !await PartnerDataService.ValidateCustomerEmailAsync(data.Email, data.CustomerID))
                ModelState.AddModelError(nameof(data.Email), "Email này đã được sử dụng bởi khách hàng khác");
            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Bổ sung khách hàng";
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                return View(data);
            }
            int id = await PartnerDataService.AddCustomerAsync(data);
            if (id <= 0)
            {
                ModelState.AddModelError("", "Không thể bổ sung khách hàng. Vui lòng thử lại sau.");
                ViewBag.Title = "Bổ sung khách hàng";
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                return View(data);
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện chỉnh sửa khách hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!ApplicationContext.HasPermission(Permissions.CustomerEdit))
                return RedirectToAction("AccessDenied", "Account");
            ViewBag.Title = "Cập nhật thông tin khách hàng";
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            var data = await PartnerDataService.GetCustomerAsync(id);
            if (data == null) return RedirectToAction("Index");
            return View(data);
        }

        /// <summary>
        /// Xử lý cập nhật thông tin khách hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Edit(Customer data)
        {
            if (!ApplicationContext.HasPermission(Permissions.CustomerEdit))
                return RedirectToAction("AccessDenied", "Account");
            if (string.IsNullOrWhiteSpace(data.CustomerName))
                ModelState.AddModelError(nameof(data.CustomerName), "Tên khách hàng không được để trống");
            if (string.IsNullOrWhiteSpace(data.ContactName))
                ModelState.AddModelError(nameof(data.ContactName), "Tên giao dịch không được để trống");
            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Email không được để trống");
            if (string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh thành");
            if (string.IsNullOrWhiteSpace(data.Address))
                ModelState.AddModelError(nameof(data.Address), "Địa chỉ không được để trống");
            if (string.IsNullOrWhiteSpace(data.Phone))
                ModelState.AddModelError(nameof(data.Phone), "Số điện thoại không được để trống");
            if (!string.IsNullOrWhiteSpace(data.Email) && !await PartnerDataService.ValidateCustomerEmailAsync(data.Email, data.CustomerID))
                ModelState.AddModelError(nameof(data.Email), "Email này đã được sử dụng bởi khách hàng khác");
            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Cập nhật thông tin khách hàng";
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                return View(data);
            }
            bool result = await PartnerDataService.UpdateCustomerAsync(data);
            if (!result)
            {
                ModelState.AddModelError("", "Không thể cập nhật thông tin khách hàng. Vui lòng thử lại sau.");
                ViewBag.Title = "Cập nhật thông tin khách hàng";
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                return View(data);
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện xác nhận xóa khách hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (!ApplicationContext.HasPermission(Permissions.CustomerDelete))
                return RedirectToAction("AccessDenied", "Account");
            ViewBag.Title = "Xóa khách hàng";
            var data = await PartnerDataService.GetCustomerAsync(id);
            if (data == null) return RedirectToAction("Index");
            return View(data);
        }

        /// <summary>
        /// Xử lý xóa khách hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id, string confirm)
        {
            if (!ApplicationContext.HasPermission(Permissions.CustomerDelete))
                return RedirectToAction("AccessDenied", "Account");
            bool result = await PartnerDataService.DeleteCustomerAsync(id);
            if (!result)
            {
                TempData["ErrorMessage"] = "Không thể xóa khách hàng này. Có thể là do khách hàng đang có dữ liệu liên quan (ví dụ: các đơn hàng).";
                return RedirectToAction("Delete", new { id = id });
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện thay đổi mật khẩu khách hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            if (!ApplicationContext.HasPermission(Permissions.CustomerChangePassword))
                return RedirectToAction("AccessDenied", "Account");
            ViewBag.Title = "Đổi mật khẩu khách hàng";
            var data = await PartnerDataService.GetCustomerAsync(id);
            if (data == null) return RedirectToAction("Index");
            return View(data);
        }

        /// <summary>
        /// Xử lý thay đổi mật khẩu khách hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            if (!ApplicationContext.HasPermission(Permissions.CustomerChangePassword))
                return RedirectToAction("AccessDenied", "Account");
            if (string.IsNullOrWhiteSpace(newPassword))
                ModelState.AddModelError("newPassword", "Vui lòng nhập mật khẩu mới");
            if (string.IsNullOrWhiteSpace(confirmPassword))
                ModelState.AddModelError("confirmPassword", "Vui lòng xác nhận lại mật khẩu");
            if (newPassword != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");
            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Đổi mật khẩu khách hàng";
                var data = await PartnerDataService.GetCustomerAsync(id);
                return View(data);
            }
            bool result = await PartnerDataService.ChangePasswordCustomerAsync(id, newPassword);
            if (!result)
            {
                ModelState.AddModelError("", "Không thể đổi mật khẩu. Vui lòng thử lại sau.");
                ViewBag.Title = "Đổi mật khẩu khách hàng";
                var data = await PartnerDataService.GetCustomerAsync(id);
                return View(data);
            }
            return RedirectToAction("Index");
        }
    }
}
