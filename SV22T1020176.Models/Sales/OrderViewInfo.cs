namespace SV22T1020176.Models.Sales
{
    /// <summary>
    /// Thông tin đầy đủ của một đơn hàng (DTO)
    /// </summary>
    public class OrderViewInfo : Order
    {
        /// <summary>
        /// Tên nhân viên phụ trách đơn hàng
        /// </summary>
        public string EmployeeName { get; set; } = "";

        /// <summary>
        /// Tên khách hàng
        /// </summary>
        public string CustomerName { get; set; } = "";
        /// <summary>
        /// Tên giao dịch của khách hàng
        /// </summary>
        public string CustomerContactName { get; set; } = "";
        /// <summary>
        /// Email của khách hàng
        /// </summary>
        public string CustomerEmail { get; set; } = "";
        /// <summary>
        /// Điện thoại khách hàng
        /// </summary>
        public string CustomerPhone { get; set; } = "";
        /// <summary>
        /// Địa chỉ của khách hàng
        /// </summary>
        public string CustomerAddress { get; set; } = "";

        /// <summary>
        /// Tên người giao hàng
        /// </summary>
        public string ShipperName { get; set; } = "";
        /// <summary>
        /// Điện thoại người giao hàng
        /// </summary>
        public string ShipperPhone { get; set; } = "";

        /// <summary>
        /// Tổng tiền (tự tính)
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Mô tả trạng thái
        /// </summary>
        public string StatusDescription
        {
            get
            {
                return Status switch
                {
                    OrderStatusEnum.Rejected => "Đã bị từ chối",
                    OrderStatusEnum.Cancelled => "Đã bị hủy",
                    OrderStatusEnum.New => "Vừa đặt, đang chờ duyệt",
                    OrderStatusEnum.Accepted => "Đã được duyệt",
                    OrderStatusEnum.Shipping => "Đang giao hàng",
                    OrderStatusEnum.Completed => "Đã giao hàng thành công",
                    _ => ""
                };
            }
        }
    }
}