using Dapper;
using SV22T1020176.DataLayers.Interfaces;
using SV22T1020176.Models.Sales;

namespace SV22T1020176.DataLayers.SqlServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu thống kê Dashboard trên SQL Server
    /// </summary>
    public class DashboardRepository : BaseSqlDAL, IDashboardRepository
    {
        public DashboardRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<int> CountCustomersAsync()
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT COUNT(*) FROM Customers WHERE IsLocked = 0 OR IsLocked IS NULL";
                return await connection.ExecuteScalarAsync<int>(sql);
            }
        }

        public async Task<int> CountProductsAsync()
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT COUNT(*) FROM Products WHERE IsSelling = 1";
                return await connection.ExecuteScalarAsync<int>(sql);
            }
        }

        public async Task<int> CountPendingOrdersAsync()
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT COUNT(*) FROM Orders WHERE Status = @Status";
                return await connection.ExecuteScalarAsync<int>(sql, new { Status = (int)OrderStatusEnum.New });
            }
        }

        public async Task<int> CountOrdersAsync()
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT COUNT(*) FROM Orders";
                return await connection.ExecuteScalarAsync<int>(sql);
            }
        }

        public async Task<decimal> GetTodayRevenueAsync()
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT ISNULL(SUM(od.Quantity * od.SalePrice), 0)
                            FROM Orders o
                            JOIN OrderDetails od ON o.OrderID = od.OrderID
                            WHERE o.Status = @CompletedStatus
                              AND CAST(COALESCE(o.FinishedTime, o.OrderTime) AS DATE) = CAST(GETDATE() AS DATE)";
                return await connection.ExecuteScalarAsync<decimal>(sql, new { CompletedStatus = (int)OrderStatusEnum.Completed });
            }
        }
        public async Task<int> GetTodayOrderCountAsync()
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT COUNT(*) FROM Orders 
                            WHERE CAST(OrderTime AS DATE) = CAST(GETDATE() AS DATE)";
                return await connection.ExecuteScalarAsync<int>(sql);
            }
        }

        public async Task<List<OrderViewInfo>> GetRecentPendingOrdersAsync(int take = 5)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT TOP (@Take) o.*,
                                   c.CustomerName, c.ContactName AS CustomerContactName,
                                   c.Phone AS CustomerPhone, c.Email AS CustomerEmail, c.Address AS CustomerAddress,
                                   e.FullName AS EmployeeName,
                                   s.ShipperName, s.Phone AS ShipperPhone,
                                   (SELECT SUM(Quantity * SalePrice) FROM OrderDetails WHERE OrderID = o.OrderID) AS TotalAmount
                            FROM Orders o
                            LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                            LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                            LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                            WHERE o.Status = @Status
                            ORDER BY o.OrderTime DESC";

                var data = (await connection.QueryAsync<OrderViewInfo>(sql, new { Status = (int)OrderStatusEnum.New, Take = take })).ToList();
                return data;
            }
        }

        public async Task<List<OrderViewInfo>> GetOrdersNeedingProcessingAsync(int take = 15)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT TOP (@Take) o.*,
                                   c.CustomerName, c.ContactName AS CustomerContactName,
                                   c.Phone AS CustomerPhone, c.Email AS CustomerEmail, c.Address AS CustomerAddress,
                                   e.FullName AS EmployeeName,
                                   s.ShipperName, s.Phone AS ShipperPhone,
                                   (SELECT SUM(Quantity * SalePrice) FROM OrderDetails WHERE OrderID = o.OrderID) AS TotalAmount
                            FROM Orders o
                            LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                            LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                            LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                            WHERE o.Status IN (@NewStatus, @AcceptedStatus)
                            ORDER BY o.OrderTime DESC";

                var data = (await connection.QueryAsync<OrderViewInfo>(sql, new
                {
                    Take = take,
                    NewStatus = (int)OrderStatusEnum.New,
                    AcceptedStatus = (int)OrderStatusEnum.Accepted
                })).ToList();
                return data;
            }
        }

        public async Task<List<ProductSalesRank>> GetTopSellingProductsAsync(int take = 4)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT TOP (@Take) p.ProductName AS ProductName, CAST(SUM(od.Quantity) AS INT) AS QuantitySold
                            FROM OrderDetails od
                            INNER JOIN Products p ON od.ProductID = p.ProductID
                            INNER JOIN Orders o ON od.OrderID = o.OrderID
                            WHERE o.Status = @CompletedStatus
                            GROUP BY p.ProductID, p.ProductName
                            ORDER BY SUM(od.Quantity) DESC";

                var data = (await connection.QueryAsync<ProductSalesRank>(sql,
                    new { Take = take, CompletedStatus = (int)OrderStatusEnum.Completed })).ToList();
                return data;
            }
        }

        public async Task<List<MonthlyRevenue>> GetMonthlyRevenueAsync(int months = 6)
        {
            using (var connection = GetConnection())
            {
                // Tổng giá trị đơn (trừ hủy / từ chối), gom theo tháng lập đơn — phù hợp CSDL nhiều đơn chưa hoàn tất
                var sql = @"SELECT
                                MONTH(o.OrderTime) AS Month,
                                YEAR(o.OrderTime) AS Year,
                                ISNULL(SUM(od.Quantity * od.SalePrice), 0) AS Revenue
                            FROM Orders o
                            JOIN OrderDetails od ON o.OrderID = od.OrderID
                            WHERE o.Status NOT IN (@Rejected, @Cancelled)
                              AND o.OrderTime >= DATEADD(MONTH, -@Months, GETDATE())
                            GROUP BY MONTH(o.OrderTime), YEAR(o.OrderTime)
                            ORDER BY Year, Month";

                var sparse = (await connection.QueryAsync<MonthlyRevenue>(sql,
                    new
                    {
                        Rejected = (int)OrderStatusEnum.Rejected,
                        Cancelled = (int)OrderStatusEnum.Cancelled,
                        Months = months
                    })).ToList();
                return FillMonthlySeries(sparse, months);
            }
        }

        /// <summary>
        /// Luôn trả đủ N tháng gần nhất (tháng không có dữ liệu = 0) để biểu đồ có trục thời gian rõ ràng.
        /// </summary>
        private static List<MonthlyRevenue> FillMonthlySeries(List<MonthlyRevenue> sparse, int months)
        {
            var result = new List<MonthlyRevenue>();
            var today = DateTime.Today;
            for (var i = months - 1; i >= 0; i--)
            {
                var d = today.AddMonths(-i);
                var hit = sparse.Find(x => x.Month == d.Month && x.Year == d.Year);
                result.Add(new MonthlyRevenue
                {
                    Month = d.Month,
                    Year = d.Year,
                    Revenue = hit?.Revenue ?? 0
                });
            }
            return result;
        }
    }
}
