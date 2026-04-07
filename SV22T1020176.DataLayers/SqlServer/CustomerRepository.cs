using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020176.DataLayers.Interfaces;
using SV22T1020176.Models.Common;
using SV22T1020176.Models.Partner;
using System.Data;

namespace SV22T1020176.DataLayers.SqlServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu trên khách hàng đối với SQL Server
    /// </summary>
    public class CustomerRepository : BaseSqlDAL, ICustomerRepository
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public CustomerRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Thêm mới một khách hàng
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<int> AddAsync(Customer data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"INSERT INTO Customers(CustomerName, ContactName, Province, Address, Phone, Email, Password, IsLocked)
                            VALUES(@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @Password, @IsLocked);
                            SELECT SCOPE_IDENTITY();";
                var parameters = new
                {
                    data.CustomerName,
                    data.ContactName,
                    data.Province,
                    data.Address,
                    data.Phone,
                    data.Email,
                    Password = data.Password ?? "",
                    data.IsLocked
                };
                var id = await connection.ExecuteScalarAsync<int>(sql, parameters);
                return id;
            }
        }

        /// <summary>
        /// Xóa một khách hàng theo mã
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = GetConnection())
            {
                var sql = @"DELETE FROM Customers WHERE CustomerID = @CustomerID";
                var parameters = new { CustomerID = id };
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Lấy thông tin một khách hàng theo mã
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Customer?> GetAsync(int id)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT * FROM Customers WHERE CustomerID = @CustomerID";
                var parameters = new { CustomerID = id };
                var cmd = new CommandDefinition(sql, parameters, commandTimeout: 120);
                var data = await connection.QueryFirstOrDefaultAsync<Customer>(cmd);
                return data;
            }
        }

        /// <summary>
        /// Kiểm tra xem khách hàng có dữ liệu liên quan hay không
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = GetConnection())
            {
                var sql = @"IF EXISTS(SELECT * FROM Orders WHERE CustomerID = @CustomerID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                var parameters = new { CustomerID = id };
                var result = await connection.ExecuteScalarAsync<int>(sql, parameters);
                return result == 1;
            }
        }

        /// <summary>
        /// Danh sách khách hàng có phân trang và tìm kiếm
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT COUNT(*) FROM Customers 
                            WHERE (@SearchValue = N'') 
                               OR (CustomerName LIKE @SearchValue) 
                               OR (ContactName LIKE @SearchValue)
                               OR (Phone LIKE @SearchValue)
                               OR (Email LIKE @SearchValue)
                               OR (Address LIKE @SearchValue);

                            SELECT * FROM Customers 
                            WHERE (@SearchValue = N'') 
                               OR (CustomerName LIKE @SearchValue) 
                               OR (ContactName LIKE @SearchValue)
                               OR (Phone LIKE @SearchValue)
                               OR (Email LIKE @SearchValue)
                               OR (Address LIKE @SearchValue)
                            ORDER BY CustomerName
                            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
                
                var parameters = new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    input.Offset,
                    input.PageSize
                };

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    var rowCount = await multi.ReadFirstAsync<int>();
                    var data = (await multi.ReadAsync<Customer>()).ToList();

                    return new PagedResult<Customer>
                    {
                        Page = input.Page,
                        PageSize = input.PageSize,
                        RowCount = rowCount,
                        DataItems = data
                    };
                }
            }
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> UpdateAsync(Customer data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"UPDATE Customers 
                            SET CustomerName = @CustomerName, 
                                ContactName = @ContactName, 
                                Province = @Province, 
                                Address = @Address, 
                                Phone = @Phone, 
                                Email = @Email, 
                                IsLocked = @IsLocked
                            WHERE CustomerID = @CustomerID";
                var parameters = new
                {
                    data.CustomerName,
                    data.ContactName,
                    data.Province,
                    data.Address,
                    data.Phone,
                    data.Email,
                    data.IsLocked,
                    data.CustomerID
                };
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Kiểm tra email có bị trùng hay không
        /// </summary>
        /// <param name="email"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using (var connection = GetConnection())
            {
                var sql = @"IF EXISTS(SELECT * FROM Customers WHERE Email = @Email AND CustomerID <> @CustomerID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                var parameters = new { Email = email, CustomerID = id };
                var result = await connection.ExecuteScalarAsync<int>(sql, parameters);
                return result == 0; // Trả về true nếu không bị trùng
            }
        }
        /// <summary>
        /// Thay đổi mật khẩu khách hàng
        /// </summary>
        /// <param name="id"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<bool> ChangePasswordAsync(int id, string password)
        {
            using (var connection = GetConnection())
            {
                var sql = @"UPDATE Customers
                            SET Password = @Password
                            WHERE CustomerID = @CustomerID";
                var parameters = new { Password = password, CustomerID = id };
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
        }
    }
}
