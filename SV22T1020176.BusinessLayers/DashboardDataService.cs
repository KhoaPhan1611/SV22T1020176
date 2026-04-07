using SV22T1020176.DataLayers.Interfaces;
using SV22T1020176.DataLayers.SqlServer;
using SV22T1020176.Models.Sales;

namespace SV22T1020176.BusinessLayers
{
    /// <summary>
    /// Cung cấp dữ liệu thống kê cho Dashboard
    /// </summary>
    public static class DashboardDataService
    {
        private static readonly IDashboardRepository dashboardDB;

        static DashboardDataService()
        {
            dashboardDB = new DashboardRepository(Configuration.ConnectionString);
        }

        /// <summary>
        /// Lấy tổng số khách hàng
        /// </summary>
        public static async Task<int> GetCustomerCountAsync()
        {
            return await dashboardDB.CountCustomersAsync();
        }

        /// <summary>
        /// Lấy tổng số sản phẩm đang bán
        /// </summary>
        public static async Task<int> GetProductCountAsync()
        {
            return await dashboardDB.CountProductsAsync();
        }

        /// <summary>
        /// Lấy số đơn hàng đang chờ xử lý
        /// </summary>
        public static async Task<int> GetPendingOrderCountAsync()
        {
            return await dashboardDB.CountPendingOrdersAsync();
        }

        /// <summary>
        /// Lấy tổng số đơn hàng
        /// </summary>
        public static async Task<int> GetOrderCountAsync()
        {
            return await dashboardDB.CountOrdersAsync();
        }

        /// <summary>
        /// Lấy doanh thu hôm nay
        /// </summary>
        public static async Task<decimal> GetTodayRevenueAsync()
        {
            return await dashboardDB.GetTodayRevenueAsync();
        }
        /// <summary>
        /// Lấy số đơn hàng hôm nay
        /// </summary>
        public static async Task<int> GetTodayOrderCountAsync()
        {
            return await dashboardDB.GetTodayOrderCountAsync();
        }

        /// <summary>
        /// Lấy danh sách đơn hàng mới cần xử lý
        /// </summary>
        public static async Task<List<OrderViewInfo>> GetRecentPendingOrdersAsync(int take = 5)
        {
            return await dashboardDB.GetRecentPendingOrdersAsync(take);
        }

        /// <summary>
        /// Đơn hàng cần xử lý (mới / chờ giao) cho dashboard
        /// </summary>
        public static async Task<List<OrderViewInfo>> GetOrdersNeedingProcessingAsync(int take = 15)
        {
            return await dashboardDB.GetOrdersNeedingProcessingAsync(take);
        }

        /// <summary>
        /// Top sản phẩm bán chạy
        /// </summary>
        public static async Task<List<ProductSalesRank>> GetTopSellingProductsAsync(int take = 4)
        {
            return await dashboardDB.GetTopSellingProductsAsync(take);
        }

        /// <summary>
        /// Lấy doanh thu theo tháng
        /// </summary>
        public static async Task<List<MonthlyRevenue>> GetMonthlyRevenueAsync(int months = 6)
        {
            return await dashboardDB.GetMonthlyRevenueAsync(months);
        }
    }
}
