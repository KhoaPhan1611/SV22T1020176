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
    /// Controller quản lý các thao tác CRUD đối với nhà cung cấp trong trang Admin.
    /// </summary>
    [Authorize]
    public class SupplierController : Controller
    {
        private const int PAGE_SIZE = 10;
        private const string SUPPLIER_SEARCH_CONDITION = "SupplierSearchCondition";

        /// <summary>
        /// Giao diện tìm kiếm và hiển thị danh sách nhà cung cấp.
        /// </summary>
        public IActionResult Index()
        {
            if (!ApplicationContext.HasPermission(Permissions.SupplierView))
                return RedirectToAction("AccessDenied", "Account");
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SUPPLIER_SEARCH_CONDITION);
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
        /// Tìm kiếm và hiển thị danh sách nhà cung cấp theo điều kiện.
        /// </summary>
        public async Task<IActionResult> Search(int page = 1, string searchValue = "")
        {
            if (!ApplicationContext.HasPermission(Permissions.SupplierView))
                return Forbid();
            var input = new PaginationSearchInput()
            {
                Page = page,
                PageSize = PAGE_SIZE,
                SearchValue = searchValue
            };
            ApplicationContext.SetSessionData(SUPPLIER_SEARCH_CONDITION, input);
            var data = await PartnerDataService.ListSuppliersAsync(input);
            return PartialView(data);
        }

        /// <summary>
        /// Giao diện thêm mới nhà cung cấp.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!ApplicationContext.HasPermission(Permissions.SupplierCreate))
                return RedirectToAction("AccessDenied", "Account");
            ViewBag.Title = "Bổ sung nhà cung cấp";
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            var model = new Supplier() { SupplierID = 0 };
            return View(model);
        }

        /// <summary>
        /// Xử lý thêm mới nhà cung cấp vào cơ sở dữ liệu.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(Supplier data)
        {
            if (!ApplicationContext.HasPermission(Permissions.SupplierCreate))
                return RedirectToAction("AccessDenied", "Account");
            if (string.IsNullOrWhiteSpace(data.SupplierName))
                ModelState.AddModelError(nameof(data.SupplierName), "Tên nhà cung cấp không được để trống");
            if (string.IsNullOrWhiteSpace(data.ContactName))
                ModelState.AddModelError(nameof(data.ContactName), "Tên giao dịch không được để trống");
            if (string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh/thành");
            if (string.IsNullOrWhiteSpace(data.Address))
                ModelState.AddModelError(nameof(data.Address), "Vui lòng nhập địa chỉ");
            if (string.IsNullOrWhiteSpace(data.Phone))
                ModelState.AddModelError(nameof(data.Phone), "Vui lòng nhập số điện thoại");
            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Bổ sung nhà cung cấp";
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                return View(data);
            }
            data.Address ??= "";
            data.Phone ??= "";
            data.Email ??= "";
            data.Province ??= "";
            int id = await PartnerDataService.AddSupplierAsync(data);
            if (id <= 0)
            {
                ModelState.AddModelError("", "Không thể bổ sung nhà cung cấp. Vui lòng thử lại sau.");
                ViewBag.Title = "Bổ sung nhà cung cấp";
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                return View(data);
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện chỉnh sửa thông tin nhà cung cấp.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!ApplicationContext.HasPermission(Permissions.SupplierEdit))
                return RedirectToAction("AccessDenied", "Account");
            ViewBag.Title = "Cập nhật thông tin nhà cung cấp";
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            var data = await PartnerDataService.GetSupplierAsync(id);
            if (data == null) return RedirectToAction("Index");
            return View(data);
        }

        /// <summary>
        /// Xử lý cập nhật thông tin nhà cung cấp trong cơ sở dữ liệu.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Edit(Supplier data)
        {
            if (!ApplicationContext.HasPermission(Permissions.SupplierEdit))
                return RedirectToAction("AccessDenied", "Account");
            if (string.IsNullOrWhiteSpace(data.SupplierName))
                ModelState.AddModelError(nameof(data.SupplierName), "Tên nhà cung cấp không được để trống");
            if (string.IsNullOrWhiteSpace(data.ContactName))
                ModelState.AddModelError(nameof(data.ContactName), "Tên giao dịch không được để trống");
            if (string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh/thành");
            if (string.IsNullOrWhiteSpace(data.Address))
                ModelState.AddModelError(nameof(data.Address), "Vui lòng nhập địa chỉ");
            if (string.IsNullOrWhiteSpace(data.Phone))
                ModelState.AddModelError(nameof(data.Phone), "Vui lòng nhập số điện thoại");
            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Cập nhật thông tin nhà cung cấp";
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                return View(data);
            }
            data.Address ??= "";
            data.Phone ??= "";
            data.Email ??= "";
            data.Province ??= "";
            bool result = await PartnerDataService.UpdateSupplierAsync(data);
            if (!result)
            {
                ModelState.AddModelError("", "Không thể cập nhật thông tin nhà cung cấp. Vui lòng thử lại sau.");
                ViewBag.Title = "Cập nhật thông tin nhà cung cấp";
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                return View(data);
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện xác nhận xóa nhà cung cấp.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (!ApplicationContext.HasPermission(Permissions.SupplierDelete))
                return RedirectToAction("AccessDenied", "Account");
            ViewBag.Title = "Xóa nhà cung cấp";
            var data = await PartnerDataService.GetSupplierAsync(id);
            if (data == null) return RedirectToAction("Index");
            return View(data);
        }

        /// <summary>
        /// Xử lý xóa nhà cung cấp khỏi cơ sở dữ liệu.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id, string confirm)
        {
            if (!ApplicationContext.HasPermission(Permissions.SupplierDelete))
                return RedirectToAction("AccessDenied", "Account");
            bool isUsed = await PartnerDataService.IsUsedSupplierAsync(id);
            if (isUsed)
            {
                TempData["ErrorMessage"] = "Không thể xóa nhà cung cấp này. Có mặt hàng đang sử dụng nhà cung cấp này.";
                return RedirectToAction("Delete", new { id = id });
            }
            bool result = await PartnerDataService.DeleteSupplierAsync(id);
            if (!result)
            {
                TempData["ErrorMessage"] = "Không thể xóa nhà cung cấp này. Vui lòng thử lại sau.";
                return RedirectToAction("Delete", new { id = id });
            }
            return RedirectToAction("Index");
        }
    }
}
