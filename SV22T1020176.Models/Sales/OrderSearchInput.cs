using SV22T1020176.Models.Common;

namespace SV22T1020176.Models.Sales
{
    /// <summary>
    /// Đầu vào tìm kiếm, phân trang đơn hàng
    /// </summary>
    public class OrderSearchInput : PaginationSearchInput
    {
        /// <summary>
        /// Trạng thái đơn hàng
        /// </summary>
        public OrderStatusEnum Status { get; set; }
        /// <summary>
        /// ID khách hàng (dùng cho Shop)
        /// </summary>
        public int? CustomerID { get; set; }
        /// <summary>
        /// Từ ngày (ngày lập đơn hàng)
        /// </summary>
        public DateTime? DateFrom { get; set; }
        /// <summary>
        /// Đến ngày (ngày lập đơn hàng)
        /// </summary>
        public DateTime? DateTo { get; set; }
    }
}