using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SV22T1020176.Admin;
using SV22T1020176.BusinessLayers;
using SV22T1020176.Models.Catalog;
using SV22T1020176.Models.Common;
using SV22T1020176.Models.Constants;

namespace SV22T1020176.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý các thao tác CRUD đối với loại hàng trong trang Admin.
    /// Phân quyền: Manager, Admin
    /// </summary>
    [Authorize]
    public class CategoryController : Controller
    {
        private const int PAGE_SIZE = 10;
        private const string CATEGORY_SEARCH_CONDITION = "CategorySearchCondition";

        /// <summary>
        /// Giao diện tìm kiếm và hiển thị danh sách loại hàng.
        /// </summary>
        public IActionResult Index()
        {
            if (!ApplicationContext.HasPermission(Permissions.CategoryView))
                return RedirectToAction("AccessDenied", "Account");
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CATEGORY_SEARCH_CONDITION);
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
        /// Tìm kiếm và hiển thị danh sách loại hàng theo điều kiện.
        /// </summary>
        public async Task<IActionResult> Search(int page = 1, string searchValue = "")
        {
            if (!ApplicationContext.HasPermission(Permissions.CategoryView))
                return Forbid();
            var input = new PaginationSearchInput()
            {
                Page = page,
                PageSize = PAGE_SIZE,
                SearchValue = searchValue
            };
            ApplicationContext.SetSessionData(CATEGORY_SEARCH_CONDITION, input);
            var data = await CatalogDataService.ListCategoriesAsync(input);
            return PartialView(data);
        }

        /// <summary>
        /// Giao diện thêm mới loại hàng.
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            if (!ApplicationContext.HasPermission(Permissions.CategoryCreate))
                return RedirectToAction("AccessDenied", "Account");
            ViewBag.Title = "Bổ sung loại hàng";
            var model = new Category() { CategoryID = 0 };
            return View(model);
        }

        /// <summary>
        /// Xử lý thêm mới loại hàng vào cơ sở dữ liệu.
        /// </summary>
        [HttpPost]
        public IActionResult Create(Category data)
        {
            if (!ApplicationContext.HasPermission(Permissions.CategoryCreate))
                return RedirectToAction("AccessDenied", "Account");
            if (string.IsNullOrWhiteSpace(data.CategoryName))
                ModelState.AddModelError(nameof(data.CategoryName), "Tên loại hàng không được để trống");
            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Bổ sung loại hàng";
                return View(data);
            }
            data.Description ??= "";
            int id = CatalogDataService.AddCategoryAsync(data).Result;
            if (id <= 0)
            {
                ModelState.AddModelError("", "Không thể bổ sung loại hàng. Vui lòng thử lại sau.");
                ViewBag.Title = "Bổ sung loại hàng";
                return View(data);
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện chỉnh sửa thông tin loại hàng.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!ApplicationContext.HasPermission(Permissions.CategoryEdit))
                return RedirectToAction("AccessDenied", "Account");
            ViewBag.Title = "Cập nhật thông tin loại hàng";
            var data = await CatalogDataService.GetCategoryAsync(id);
            if (data == null) return RedirectToAction("Index");
            return View(data);
        }

        /// <summary>
        /// Xử lý cập nhật thông tin loại hàng trong cơ sở dữ liệu.
        /// </summary>
        [HttpPost]
        public IActionResult Edit(Category data)
        {
            if (!ApplicationContext.HasPermission(Permissions.CategoryEdit))
                return RedirectToAction("AccessDenied", "Account");
            if (string.IsNullOrWhiteSpace(data.CategoryName))
                ModelState.AddModelError(nameof(data.CategoryName), "Tên loại hàng không được để trống");
            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Cập nhật thông tin loại hàng";
                return View(data);
            }
            data.Description ??= "";
            bool result = CatalogDataService.UpdateCategoryAsync(data).Result;
            if (!result)
            {
                ModelState.AddModelError("", "Không thể cập nhật thông tin loại hàng. Vui lòng thử lại sau.");
                ViewBag.Title = "Cập nhật thông tin loại hàng";
                return View(data);
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện xác nhận xóa loại hàng.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (!ApplicationContext.HasPermission(Permissions.CategoryDelete))
                return RedirectToAction("AccessDenied", "Account");
            ViewBag.Title = "Xóa loại hàng";
            var data = await CatalogDataService.GetCategoryAsync(id);
            if (data == null) return RedirectToAction("Index");
            return View(data);
        }

        /// <summary>
        /// Xử lý xóa loại hàng khỏi cơ sở dữ liệu.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id, string confirm)
        {
            if (!ApplicationContext.HasPermission(Permissions.CategoryDelete))
                return RedirectToAction("AccessDenied", "Account");
            bool isUsed = await CatalogDataService.IsUsedCategoryAsync(id);
            if (isUsed)
            {
                TempData["ErrorMessage"] = "Không thể xóa loại hàng này. Có mặt hàng đang thuộc loại hàng này.";
                return RedirectToAction("Delete", new { id = id });
            }
            bool result = await CatalogDataService.DeleteCategoryAsync(id);
            if (!result)
            {
                TempData["ErrorMessage"] = "Không thể xóa loại hàng này. Vui lòng thử lại sau.";
                return RedirectToAction("Delete", new { id = id });
            }
            return RedirectToAction("Index");
        }
    }
}
