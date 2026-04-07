using Microsoft.AspNetCore.Mvc;
using SV22T1020176.DataLayers.Interfaces;
using SV22T1020176.Models.Common;

namespace SV22T1020176.Admin.Controllers
{
    /// <summary>
    /// Controller để test chức năng lấy dữ liệu với phân trang và tìm kiếm
    /// </summary>
    public class TestController : Controller
    {
        private readonly ICustomerRepository _customerRepository;

        /// <summary>
        /// Constructor nhận repository thông qua Dependency Injection
        /// </summary>
        /// <param name="customerRepository"></param>
        public TestController(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        /// <summary>
        /// Action Index nhận PaginationSearchInput và gọi Repository để lấy dữ liệu
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IActionResult> Index(PaginationSearchInput input)
        {
            // Thiết lập giá trị mặc định nếu chưa có
            if (input.PageSize <= 0) input.PageSize = 10;
            if (input.Page <= 0) input.Page = 1;

            // Gọi repository để lấy dữ liệu phân trang
            var result = await _customerRepository.ListAsync(input);

            // Trả về view kèm theo dữ liệu
            return View(result);
        }
    }
}
