using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020176.DataLayers.Interfaces;
using SV22T1020176.Models.Common;
using SV22T1020176.Models.HR;
using System.Data;

namespace SV22T1020176.DataLayers.SqlServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu trên nhân viên đối với SQL Server
    /// </summary>
    public class EmployeeRepository : BaseSqlDAL, IEmployeeRepository
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public EmployeeRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Thêm mới nhân viên
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<int> AddAsync(Employee data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"INSERT INTO Employees(FullName, BirthDate, Address, Phone, Email, Password, Photo, IsWorking, RoleNames)
                            VALUES(@FullName, @BirthDate, @Address, @Phone, @Email, @Password, @Photo, @IsWorking, @RoleNames);
                            SELECT SCOPE_IDENTITY();";
                var parameters = new
                {
                    data.FullName,
                    data.BirthDate,
                    data.Address,
                    data.Phone,
                    data.Email,
                    Password = data.Password ?? "",
                    data.Photo,
                    data.IsWorking,
                    data.RoleNames
                };
                var id = await connection.ExecuteScalarAsync<int>(sql, parameters);
                return id;
            }
        }

        /// <summary>
        /// Xóa nhân viên
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = GetConnection())
            {
                var sql = @"DELETE FROM Employees WHERE EmployeeID = @EmployeeID";
                var parameters = new { EmployeeID = id };
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Lấy thông tin một nhân viên
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Employee?> GetAsync(int id)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT * FROM Employees WHERE EmployeeID = @EmployeeID";
                var parameters = new { EmployeeID = id };
                var data = await connection.QueryFirstOrDefaultAsync<Employee>(sql, parameters);
                return data;
            }
        }

        /// <summary>
        /// Kiểm tra nhân viên có dữ liệu liên quan không
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = GetConnection())
            {
                var sql = @"IF EXISTS(SELECT * FROM Orders WHERE EmployeeID = @EmployeeID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                var parameters = new { EmployeeID = id };
                var result = await connection.ExecuteScalarAsync<int>(sql, parameters);
                return result == 1;
            }
        }

        /// <summary>
        /// Danh sách nhân viên có phân trang và tìm kiếm
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            using (var connection = GetConnection())
            {
                var sql = @"SELECT COUNT(*) FROM Employees 
                            WHERE (@SearchValue = N'') 
                               OR (FullName LIKE @SearchValue) 
                               OR (Phone LIKE @SearchValue) 
                               OR (Email LIKE @SearchValue)
                               OR (Address LIKE @SearchValue);

                            SELECT * FROM Employees 
                            WHERE (@SearchValue = N'') 
                               OR (FullName LIKE @SearchValue) 
                               OR (Phone LIKE @SearchValue) 
                               OR (Email LIKE @SearchValue)
                               OR (Address LIKE @SearchValue)
                            ORDER BY FullName
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
                    var data = (await multi.ReadAsync<Employee>()).ToList();

                    return new PagedResult<Employee>
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
        /// Cập nhật thông tin nhân viên
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> UpdateAsync(Employee data)
        {
            using (var connection = GetConnection())
            {
                var sql = @"UPDATE Employees 
                            SET FullName = @FullName, 
                                BirthDate = @BirthDate, 
                                Address = @Address, 
                                Phone = @Phone, 
                                Email = @Email, 
                                Photo = @Photo, 
                                IsWorking = @IsWorking, 
                                RoleNames = @RoleNames
                            WHERE EmployeeID = @EmployeeID";
                var parameters = new
                {
                    data.FullName,
                    data.BirthDate,
                    data.Address,
                    data.Phone,
                    data.Email,
                    data.Photo,
                    data.IsWorking,
                    data.RoleNames,
                    data.EmployeeID
                };
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Kiểm tra email trùng
        /// </summary>
        /// <param name="email"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using (var connection = GetConnection())
            {
                var sql = @"IF EXISTS(SELECT * FROM Employees WHERE Email = @Email AND EmployeeID <> @EmployeeID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                var parameters = new { Email = email, EmployeeID = id };
                var result = await connection.ExecuteScalarAsync<int>(sql, parameters);
                return result == 0;
            }
        }

        /// <summary>
        /// Cập nhật mật khẩu nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <param name="password">Mật khẩu mới</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> ChangePasswordAsync(int id, string password)
        {
            using (var connection = GetConnection())
            {
                var sql = @"UPDATE Employees SET Password = @Password WHERE EmployeeID = @EmployeeID";
                var parameters = new { EmployeeID = id, Password = password ?? "" };
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Cập nhật quyền của nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <param name="roleNames">Danh sách quyền (phân cách bởi dấu phẩy)</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateRoleNamesAsync(int id, string roleNames)
        {
            using (var connection = GetConnection())
            {
                var sql = @"UPDATE Employees SET RoleNames = @RoleNames WHERE EmployeeID = @EmployeeID";
                var parameters = new { EmployeeID = id, RoleNames = roleNames ?? "" };
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
        }
    }
}
