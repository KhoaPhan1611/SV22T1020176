using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020176.DataLayers.Interfaces;
using SV22T1020176.Models.Common;
using SV22T1020176.Models.Sales;
using System.Data;

namespace SV22T1020176.DataLayers.SqlServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu đơn hàng trên SQL Server
    /// </summary>
    public class OrderRepository : BaseSqlDAL, IOrderRepository
    {
        public OrderRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<int> AddAsync(Order data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"INSERT INTO Orders(CustomerID, OrderTime, DeliveryProvince, DeliveryAddress, EmployeeID, Status)
                            VALUES(@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress, @EmployeeID, @Status);
                            SELECT SCOPE_IDENTITY();";
                var id = await connection.ExecuteScalarAsync<int>(sql, data);
                return id;
            }
        }

        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"INSERT INTO OrderDetails(OrderID, ProductID, Quantity, SalePrice)
                            VALUES(@OrderID, @ProductID, @Quantity, @SalePrice)";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeleteAsync(int orderID)
        {
            using (var connection = GetConnection())
            {
                var sql = @"DELETE FROM OrderDetails WHERE OrderID = @OrderID;
                            DELETE FROM Orders WHERE OrderID = @OrderID;";
                var rowsAffected = await connection.ExecuteAsync(sql, new { OrderID = orderID });
                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using (var connection = GetConnection())
            {
                var sql = @"DELETE FROM OrderDetails WHERE OrderID = @OrderID AND ProductID = @ProductID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { OrderID = orderID, ProductID = productID });
                return rowsAffected > 0;
            }
        }

        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT o.*, 
                                   c.CustomerName, c.ContactName AS CustomerContactName, c.Phone AS CustomerPhone, c.Email AS CustomerEmail, c.Address AS CustomerAddress,
                                   e.FullName AS EmployeeName,
                                   s.ShipperName, s.Phone AS ShipperPhone
                            FROM Orders AS o
                            LEFT JOIN Customers AS c ON o.CustomerID = c.CustomerID
                            LEFT JOIN Employees AS e ON o.EmployeeID = e.EmployeeID
                            LEFT JOIN Shippers AS s ON o.ShipperID = s.ShipperID
                            WHERE o.OrderID = @OrderID";
                var data = await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { OrderID = orderID });
                return data;
            }
        }

        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT od.*, p.ProductName, p.Photo, p.Unit
                            FROM OrderDetails AS od
                            JOIN Products AS p ON od.ProductID = p.ProductID
                            WHERE od.OrderID = @OrderID AND od.ProductID = @ProductID";
                var data = await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID, ProductID = productID });
                return data;
            }
        }

        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT COUNT(*) FROM Orders AS o
                            LEFT JOIN Customers AS c ON o.CustomerID = c.CustomerID
                            WHERE (@Status = 0 OR o.Status = @Status)
                                AND (@CustomerID IS NULL OR o.CustomerID = @CustomerID)
                                AND (@SearchValue = N'' OR c.CustomerName LIKE @SearchValue OR o.DeliveryProvince LIKE @SearchValue OR o.DeliveryAddress LIKE @SearchValue)
                                AND (@DateFrom IS NULL OR CAST(o.OrderTime AS DATE) >= CAST(@DateFrom AS DATE))
                                AND (@DateTo IS NULL OR CAST(o.OrderTime AS DATE) <= CAST(@DateTo AS DATE));

                            SELECT o.*, 
                                   c.CustomerName, c.ContactName AS CustomerContactName, c.Phone AS CustomerPhone, c.Email AS CustomerEmail, c.Address AS CustomerAddress,
                                   e.FullName AS EmployeeName,
                                   s.ShipperName, s.Phone AS ShipperPhone,
                                   (SELECT SUM(Quantity * SalePrice) FROM OrderDetails WHERE OrderID = o.OrderID) AS TotalAmount
                            FROM Orders AS o
                            LEFT JOIN Customers AS c ON o.CustomerID = c.CustomerID
                            LEFT JOIN Employees AS e ON o.EmployeeID = e.EmployeeID
                            LEFT JOIN Shippers AS s ON o.ShipperID = s.ShipperID
                            WHERE (@Status = 0 OR o.Status = @Status)
                                AND (@CustomerID IS NULL OR o.CustomerID = @CustomerID)
                                AND (@SearchValue = N'' OR c.CustomerName LIKE @SearchValue OR o.DeliveryProvince LIKE @SearchValue OR o.DeliveryAddress LIKE @SearchValue)
                                AND (@DateFrom IS NULL OR CAST(o.OrderTime AS DATE) >= CAST(@DateFrom AS DATE))
                                AND (@DateTo IS NULL OR CAST(o.OrderTime AS DATE) <= CAST(@DateTo AS DATE))
                            ORDER BY o.OrderTime DESC
                            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                var parameters = new
                {
                    input.Status,
                    input.CustomerID,
                    SearchValue = $"%{input.SearchValue}%",
                    input.DateFrom,
                    input.DateTo,
                    input.Offset,
                    input.PageSize
                };

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    var rowCount = await multi.ReadFirstAsync<int>();
                    var data = (await multi.ReadAsync<OrderViewInfo>()).ToList();

                    return new PagedResult<OrderViewInfo>
                    {
                        Page = input.Page,
                        PageSize = input.PageSize,
                        RowCount = rowCount,
                        DataItems = data
                    };
                }
            }
        }

        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT od.*, p.ProductName, p.Photo, p.Unit
                            FROM OrderDetails AS od
                            JOIN Products AS p ON od.ProductID = p.ProductID
                            WHERE od.OrderID = @OrderID";
                var data = (await connection.QueryAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID })).ToList();
                return data;
            }
        }

        public async Task<bool> UpdateAsync(Order data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"UPDATE Orders 
                            SET CustomerID = @CustomerID,
                                DeliveryProvince = @DeliveryProvince,
                                DeliveryAddress = @DeliveryAddress,
                                EmployeeID = @EmployeeID,
                                AcceptTime = @AcceptTime,
                                ShipperID = @ShipperID,
                                ShippedTime = @ShippedTime,
                                FinishedTime = @FinishedTime,
                                Status = @Status
                            WHERE OrderID = @OrderID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }

        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"UPDATE OrderDetails 
                            SET Quantity = @Quantity, SalePrice = @SalePrice
                            WHERE OrderID = @OrderID AND ProductID = @ProductID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }
    }
}
