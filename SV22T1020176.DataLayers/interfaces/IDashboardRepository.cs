namespace SV22T1020176.DataLayers.Interfaces
{
    /// <summary>
    /// Repository cung cấp dữ liệu thống kê cho Dashboard
    /// </summary>
    public interface IDashboardRepository
    {
        /// <summary>
        /// Đếm tổng số khách hàng
        /// </summary>
        Task<int> CountCustomersAsync();

        /// <summary>
        /// Đếm tổng số sản phẩm đang bán
        /// </summary>
        Task<int> CountProductsAsync();

        /// <summary>
        /// Đếm tổng số đơn hàng đang chờ xử lý (trạng thái New)
        /// </summary>
        Task<int> CountPendingOrdersAsync();

        /// <summary>
        /// Đếm tổng số đơn hàng trong hệ thống
        /// </summary>
        Task<int> CountOrdersAsync();

        /// <summary>
        /// Tính tổng doanh thu hôm nay
        /// </summary>
        Task<decimal> GetTodayRevenueAsync();
        /// <summary>
        /// Đếm số đơn hàng hôm nay
        /// </summary>
        Task<int> GetTodayOrderCountAsync();

        /// <summary>
        /// Lấy danh sách đơn hàng mới cần xử lý (phân trang)
        /// </summary>
        Task<List<SV22T1020176.Models.Sales.OrderViewInfo>> GetRecentPendingOrdersAsync(int take = 5);

        /// <summary>
        /// Đơn hàng cần xử lý (mới / đã duyệt chờ giao) cho bảng dashboard
        /// </summary>
        Task<List<SV22T1020176.Models.Sales.OrderViewInfo>> GetOrdersNeedingProcessingAsync(int take = 15);

        /// <summary>
        /// Top sản phẩm bán chạy (theo số lượng đã bán trên đơn hoàn tất)
        /// </summary>
        Task<List<ProductSalesRank>> GetTopSellingProductsAsync(int take = 4);

        /// <summary>
        /// Tổng giá trị đơn hàng theo tháng lập đơn (loại trừ đơn hủy / từ chối), dùng cho biểu đồ dashboard
        /// </summary>
        Task<List<MonthlyRevenue>> GetMonthlyRevenueAsync(int months = 6);
    }

    /// <summary>
    /// DTO cho doanh thu theo tháng
    /// </summary>
    public class MonthlyRevenue
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Revenue { get; set; }
        public string Label => $"{Month}/{Year}";
    }

    /// <summary>
    /// Sản phẩm xếp hạng theo số lượng bán
    /// </summary>
    public class ProductSalesRank
    {
        public string ProductName { get; set; } = "";
        public int QuantitySold { get; set; }
    }
}
