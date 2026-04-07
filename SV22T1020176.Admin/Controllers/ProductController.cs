using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SV22T1020176.Admin;
using SV22T1020176.BusinessLayers;
using SV22T1020176.Models.Catalog;
using SV22T1020176.Models.Common;
using SV22T1020176.Models.Constants;
using SV22T1020176.Admin.AppCodes;

namespace SV22T1020176.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý các thao tác CRUD đối với mặt hàng trong trang Admin.
    /// Phân quyền: Manager, Admin, Inventory
    /// </summary>
    [Authorize]
    public class ProductController : Controller
    {
        private const int PAGE_SIZE = 12;
        private const string PRODUCT_SEARCH_CONDITION = "ProductSearchCondition";
        private const string ViewDataKeyListPhotosStandalone = "ProductListPhotosStandalone";
        private const string ViewDataKeyListAttributesStandalone = "ProductListAttributesStandalone";

        private static IActionResult RedirectToEditWithFragment(Controller controller, int productId, string fragment)
        {
            var url = controller.Url.Action(nameof(Edit), values: new { id = productId });
            if (string.IsNullOrEmpty(url))
                return controller.RedirectToAction(nameof(Edit), new { id = productId });
            return controller.LocalRedirect(url + fragment);
        }

        /// <summary>
        /// Giao diện tìm kiếm và hiển thị danh sách mặt hàng.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductView))
                return RedirectToAction("AccessDenied", "Account");
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH_CONDITION);
            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = PAGE_SIZE,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };
            }
            ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và hiển thị danh sách mặt hàng theo điều kiện.
        /// </summary>
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductView))
                return Forbid();
            input.PageSize = PAGE_SIZE;
            ApplicationContext.SetSessionData(PRODUCT_SEARCH_CONDITION, input);
            var data = await CatalogDataService.ListProductsAsync(input);
            return PartialView(data);
        }

        /// <summary>
        /// Giao diện xem thông tin chi tiết của mặt hàng.
        /// </summary>
        public async Task<IActionResult> Detail(int id)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductView))
                return RedirectToAction("AccessDenied", "Account");
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");

            var categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            var suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });

            if (product.CategoryID.HasValue && product.CategoryID > 0)
                ViewBag.CategoryName = categories.DataItems.FirstOrDefault(c => c.CategoryID == product.CategoryID)?.CategoryName ?? "";
            else
                ViewBag.CategoryName = "";
            if (product.SupplierID.HasValue && product.SupplierID > 0)
                ViewBag.SupplierName = suppliers.DataItems.FirstOrDefault(s => s.SupplierID == product.SupplierID)?.SupplierName ?? "";
            else
                ViewBag.SupplierName = "";
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);
            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            return View(product);
        }

        /// <summary>
        /// Giao diện thêm mới mặt hàng.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductCreate))
                return RedirectToAction("AccessDenied", "Account");
            ViewBag.Title = "Bổ sung mặt hàng";
            ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            var model = new Product() { ProductID = 0, IsSelling = true, Unit = "", Price = 0 };
            return View(model);
        }

        /// <summary>
        /// Xử lý thêm mới mặt hàng vào cơ sở dữ liệu.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(Product data, IFormFile? photoFile,
                                              List<IFormFile>? galleryPhotos, List<string>? galleryDescriptions,
                                              List<string>? attrNames, List<string>? attrValues)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductCreate))
                return RedirectToAction("AccessDenied", "Account");

            // Validation (giữ nguyên các kiểm tra cơ bản)
            if (string.IsNullOrWhiteSpace(data.ProductName))
                ModelState.AddModelError(nameof(data.ProductName), "Tên mặt hàng không được để trống");
            if (data.CategoryID == null || data.CategoryID <= 0)
                ModelState.AddModelError("CategoryID", "Vui lòng chọn loại hàng");
            if (data.SupplierID == null || data.SupplierID <= 0)
                ModelState.AddModelError("SupplierID", "Vui lòng chọn nhà cung cấp");
            if (string.IsNullOrWhiteSpace(data.Unit))
                ModelState.AddModelError(nameof(data.Unit), "Đơn vị tính không được để trống");
            if (data.Price < 0)
                ModelState.AddModelError(nameof(data.Price), "Giá phải lớn hơn hoặc bằng 0");
            if (photoFile == null || photoFile.Length == 0)
                ModelState.AddModelError("Photo", "Vui lòng chọn ảnh đại diện cho mặt hàng");
            if (string.IsNullOrWhiteSpace(data.ProductDescription))
                ModelState.AddModelError(nameof(data.ProductDescription), "Vui lòng nhập mô tả mặt hàng");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Bổ sung mặt hàng";
                ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
                ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
                return View(data);
            }

            // 1. Xử lý ảnh đại diện chính
            data.ProductDescription ??= "";
            if (photoFile != null && photoFile.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(photoFile.FileName ?? ".jpg")}";
                var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images", "products", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photoFile.CopyToAsync(stream);
                }
                data.Photo = fileName;
            }
            else data.Photo = "";

            // 2. Lưu mặt hàng chính
            int productId = await CatalogDataService.AddProductAsync(data);
            if (productId <= 0)
            {
                ModelState.AddModelError("", "Không thể bổ sung mặt hàng. Vui lòng thử lại sau.");
                ViewBag.Title = "Bổ sung mặt hàng";
                ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
                ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
                return View(data);
            }

            // 3. Xử lý lưu các ảnh thư viện đi kèm (nếu có)
            if (galleryPhotos != null && galleryPhotos.Count > 0)
            {
                for (int i = 0; i < galleryPhotos.Count; i++)
                {
                    var file = galleryPhotos[i];
                    if (file.Length > 0)
                    {
                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName ?? ".jpg")}";
                        var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images", "products", fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var photoData = new ProductPhoto()
                        {
                            ProductID = productId,
                            Photo = fileName,
                            Description = (galleryDescriptions != null && galleryDescriptions.Count > i) ? galleryDescriptions[i] : "",
                            DisplayOrder = i + 1,
                            IsHidden = false
                        };
                        await CatalogDataService.AddPhotoAsync(photoData);
                    }
                }
            }

            // 4. Xử lý lưu các thuộc tính đi kèm (nếu có)
            if (attrNames != null && attrValues != null)
            {
                for (int i = 0; i < Math.Min(attrNames.Count, attrValues.Count); i++)
                {
                    if (!string.IsNullOrWhiteSpace(attrNames[i]))
                    {
                        var attrData = new ProductAttribute()
                        {
                            ProductID = productId,
                            AttributeName = attrNames[i],
                            AttributeValue = attrValues[i],
                            DisplayOrder = i + 1
                        };
                        await CatalogDataService.AddAttributeAsync(attrData);
                    }
                }
            }

            TempData["SuccessMessage"] = $"Đã thêm mới mặt hàng '{data.ProductName}' thành công.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện chỉnh sửa thông tin mặt hàng.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductEdit))
                return RedirectToAction("AccessDenied", "Account");
            ViewBag.Title = "Cập nhật thông tin mặt hàng";
            ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            var data = await CatalogDataService.GetProductAsync(id);
            if (data == null) return RedirectToAction("Index");
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);
            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            return View(data);
        }

        /// <summary>
        /// Xử lý cập nhật thông tin mặt hàng trong cơ sở dữ liệu.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Edit(Product data, IFormFile? photoFile)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductEdit))
                return RedirectToAction("AccessDenied", "Account");
            if (string.IsNullOrWhiteSpace(data.ProductName))
                ModelState.AddModelError(nameof(data.ProductName), "Tên mặt hàng không được để trống");
            if (data.CategoryID == null || data.CategoryID <= 0)
                ModelState.AddModelError("CategoryID", "Vui lòng chọn loại hàng");
            if (data.SupplierID == null || data.SupplierID <= 0)
                ModelState.AddModelError("SupplierID", "Vui lòng chọn nhà cung cấp");
            if (string.IsNullOrWhiteSpace(data.Unit))
                ModelState.AddModelError(nameof(data.Unit), "Đơn vị tính không được để trống");
            if (data.Price < 0)
                ModelState.AddModelError(nameof(data.Price), "Giá phải lớn hơn hoặc bằng 0");
            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Cập nhật thông tin mặt hàng";
                ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
                ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
                ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(data.ProductID);
                ViewBag.Photos = await CatalogDataService.ListPhotosAsync(data.ProductID);
                return View(data);
            }
            data.ProductDescription ??= "";
            if (photoFile != null && photoFile.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(photoFile.FileName ?? ".jpg")}";
                var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images", "products", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photoFile.CopyToAsync(stream);
                }
                data.Photo = fileName;
            }
            else
            {
                data.Photo ??= "";
            }
            bool result = await CatalogDataService.UpdateProductAsync(data);
            if (!result)
            {
                ModelState.AddModelError("", "Không thể cập nhật thông tin mặt hàng. Vui lòng thử lại sau.");
                ViewBag.Title = "Cập nhật thông tin mặt hàng";
                ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
                ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
                ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(data.ProductID);
                ViewBag.Photos = await CatalogDataService.ListPhotosAsync(data.ProductID);
                return View(data);
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện xác nhận xóa mặt hàng.
        /// </summary>
        [HttpGet]
        [AuthorizePermission(Permissions.ProductDelete)]
        public async Task<IActionResult> Delete(int id)
        {
            ViewBag.Title = "Xóa mặt hàng";
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");
            var categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            var suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
            if (product.CategoryID.HasValue && product.CategoryID > 0)
                ViewBag.CategoryName = categories.DataItems.FirstOrDefault(c => c.CategoryID == product.CategoryID)?.CategoryName ?? "";
            else
                ViewBag.CategoryName = "";
            if (product.SupplierID.HasValue && product.SupplierID > 0)
                ViewBag.SupplierName = suppliers.DataItems.FirstOrDefault(s => s.SupplierID == product.SupplierID)?.SupplierName ?? "";
            else
                ViewBag.SupplierName = "";
            return View(product);
        }

        /// <summary>
        /// Xử lý xóa mặt hàng khỏi cơ sở dữ liệu.
        /// </summary>
        [HttpPost]
        [AuthorizePermission(Permissions.ProductDelete)]
        public async Task<IActionResult> Delete(int id, string confirm)
        {
            bool isUsed = await CatalogDataService.IsUsedProductAsync(id);
            if (isUsed)
            {
                TempData["ErrorMessage"] = "Không thể xóa mặt hàng này. Có đơn hàng đang sử dụng mặt hàng này.";
                return RedirectToAction("Delete", new { id = id });
            }
            bool result = await CatalogDataService.DeleteProductAsync(id);
            if (!result)
            {
                TempData["ErrorMessage"] = "Không thể xóa mặt hàng này. Vui lòng thử lại sau.";
                return RedirectToAction("Delete", new { id = id });
            }
            return RedirectToAction("Index");
        }

        #region Product Attributes

        [HttpGet]
        public async Task<IActionResult> ListAttributes(int id)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductView))
                return RedirectToAction("AccessDenied", "Account");
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");
            var attributes = await CatalogDataService.ListAttributesAsync(id);
            ViewBag.ProductID = id;
            ViewBag.Title = "Thuộc tính mặt hàng";
            ViewData[ViewDataKeyListAttributesStandalone] = true;
            return View(attributes);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAttribute(int id)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductManageAttribute))
                return RedirectToAction("AccessDenied", "Account");
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");
            ViewBag.ProductName = product.ProductName;

            var attributes = await CatalogDataService.ListAttributesAsync(id);
            int nextOrder = attributes.Any() ? attributes.Max(a => a.DisplayOrder) + 1 : 1;

            var model = new ProductAttribute() { ProductID = id, DisplayOrder = nextOrder };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAttribute(ProductAttribute data)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductManageAttribute))
                return RedirectToAction("AccessDenied", "Account");
            if (string.IsNullOrWhiteSpace(data.AttributeName))
                ModelState.AddModelError(nameof(data.AttributeName), "Tên thuộc tính không được để trống");
            if (string.IsNullOrWhiteSpace(data.AttributeValue))
                ModelState.AddModelError(nameof(data.AttributeValue), "Giá trị thuộc tính không được để trống");
            if (!ModelState.IsValid)
            {
                var product = await CatalogDataService.GetProductAsync(data.ProductID);
                if (product != null) ViewBag.ProductName = product.ProductName;
                return View(data);
            }
            await CatalogDataService.AddAttributeAsync(data);
            return RedirectToEditWithFragment(this, data.ProductID, "#attributes");
        }

        [HttpGet]
        public async Task<IActionResult> EditAttribute(int id, long attributeId)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductManageAttribute))
                return RedirectToAction("AccessDenied", "Account");
            var attribute = await CatalogDataService.GetAttributeAsync(attributeId);
            if (attribute == null) return RedirectToEditWithFragment(this, id, "#attributes");
            var product = await CatalogDataService.GetProductAsync(id);
            if (product != null) ViewBag.ProductName = product.ProductName;
            return View(attribute);
        }

        [HttpPost]
        public async Task<IActionResult> EditAttribute(ProductAttribute data)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductManageAttribute))
                return RedirectToAction("AccessDenied", "Account");
            if (string.IsNullOrWhiteSpace(data.AttributeName))
                ModelState.AddModelError(nameof(data.AttributeName), "Tên thuộc tính không được để trống");
            if (string.IsNullOrWhiteSpace(data.AttributeValue))
                ModelState.AddModelError(nameof(data.AttributeValue), "Giá trị thuộc tính không được để trống");
            if (!ModelState.IsValid)
            {
                var product = await CatalogDataService.GetProductAsync(data.ProductID);
                if (product != null) ViewBag.ProductName = product.ProductName;
                return View(data);
            }
            await CatalogDataService.UpdateAttributeAsync(data);
            return RedirectToEditWithFragment(this, data.ProductID, "#attributes");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteAttribute(int id, long attributeId)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductManageAttribute))
                return RedirectToAction("AccessDenied", "Account");
            var attribute = await CatalogDataService.GetAttributeAsync(attributeId);
            if (attribute == null) return RedirectToEditWithFragment(this, id, "#attributes");
            var product = await CatalogDataService.GetProductAsync(id);
            if (product != null) ViewBag.ProductName = product.ProductName;
            return View(attribute);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAttribute(int id, long attributeId, string confirm)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductManageAttribute))
                return RedirectToAction("AccessDenied", "Account");
            await CatalogDataService.DeleteAttributeAsync(attributeId);
            return RedirectToEditWithFragment(this, id, "#attributes");
        }

        #endregion

        #region Product Photos

        [HttpGet]
        public async Task<IActionResult> ListPhotos(int id)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductView))
                return RedirectToAction("AccessDenied", "Account");
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");
            var photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.ProductID = id;
            ViewBag.Title = "Thư viện ảnh";
            ViewData[ViewDataKeyListPhotosStandalone] = true;
            return View(photos);
        }

        [HttpGet]
        public async Task<IActionResult> CreatePhoto(int id)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductManagePhoto))
                return RedirectToAction("AccessDenied", "Account");
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");
            ViewBag.ProductName = product.ProductName;

            var photos = await CatalogDataService.ListPhotosAsync(id);
            int nextOrder = photos.Any() ? photos.Max(p => p.DisplayOrder) + 1 : 1;

            var model = new ProductPhoto() { ProductID = id, DisplayOrder = nextOrder, IsHidden = false };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePhoto(ProductPhoto data, IFormFile? photoFile)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductManagePhoto))
                return RedirectToAction("AccessDenied", "Account");

            // Kiểm tra file ảnh riêng vì nó không nằm trong model ProductPhoto trực tiếp
            if (photoFile == null || photoFile.Length == 0)
                ModelState.AddModelError("Photo", "Vui lòng chọn file ảnh để upload.");

            if (!ModelState.IsValid)
            {
                var product = await CatalogDataService.GetProductAsync(data.ProductID);
                if (product != null) ViewBag.ProductName = product.ProductName;
                return View(data);
            }

            // Xử lý upload ảnh
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(photoFile!.FileName ?? ".jpg")}";
            var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images", "products", fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photoFile!.CopyToAsync(stream);
            }

            // Cập nhật tên file vào model và lưu
            data.Photo = fileName;
            await CatalogDataService.AddPhotoAsync(data);
            
            return RedirectToEditWithFragment(this, data.ProductID, "#photos");
        }

        [HttpGet]
        public async Task<IActionResult> EditPhoto(int id, long photoId)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductManagePhoto))
                return RedirectToAction("AccessDenied", "Account");
            var photo = await CatalogDataService.GetPhotoAsync(photoId);
            if (photo == null) return RedirectToEditWithFragment(this, id, "#photos");
            var product = await CatalogDataService.GetProductAsync(id);
            if (product != null) ViewBag.ProductName = product.ProductName;
            return View(photo);
        }

        [HttpPost]
        public async Task<IActionResult> EditPhoto(ProductPhoto data, IFormFile? photoFile)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductManagePhoto))
                return RedirectToAction("AccessDenied", "Account");
            if (!ModelState.IsValid)
            {
                var product = await CatalogDataService.GetProductAsync(data.ProductID);
                if (product != null) ViewBag.ProductName = product.ProductName;
                return View(data);
            }
            if (photoFile != null && photoFile.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(photoFile!.FileName ?? ".jpg")}";
                var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images", "products", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photoFile!.CopyToAsync(stream);
                }
                data.Photo = fileName;
            }
            data.Description ??= "";
            await CatalogDataService.UpdatePhotoAsync(data);
            return RedirectToEditWithFragment(this, data.ProductID, "#photos");
        }

        [HttpGet]
        public async Task<IActionResult> DeletePhoto(int id, long photoId)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductManagePhoto))
                return RedirectToAction("AccessDenied", "Account");
            var photo = await CatalogDataService.GetPhotoAsync(photoId);
            if (photo == null) return RedirectToEditWithFragment(this, id, "#photos");
            var product = await CatalogDataService.GetProductAsync(id);
            if (product != null) ViewBag.ProductName = product.ProductName;
            return View(photo);
        }

        [HttpPost]
        public async Task<IActionResult> DeletePhoto(int id, long photoId, string confirm)
        {
            if (!ApplicationContext.HasPermission(Permissions.ProductManagePhoto))
                return RedirectToAction("AccessDenied", "Account");
            await CatalogDataService.DeletePhotoAsync(photoId);
            return RedirectToEditWithFragment(this, id, "#photos");
        }

        #endregion
    }
}
