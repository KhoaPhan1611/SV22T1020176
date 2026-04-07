using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020176.DataLayers.Interfaces;
using SV22T1020176.Models.Security;
using System.Data;

namespace SV22T1020176.DataLayers.SqlServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến tài khoản người dùng đối với SQL Server
    /// </summary>
    public class UserAccountRepository : BaseSqlDAL, IUserAccountRepository
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public UserAccountRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Xác thực tài khoản người dùng
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using (var connection = GetConnection())
            {
                // Lưu ý: Trong thực tế nên hash mật khẩu. Ở đây làm theo database hiện tại.
                var sql = @"SELECT EmployeeID AS UserID, Email AS UserName, FullName, Photo, RoleNames 
                            FROM Employees 
                            WHERE Email = @Email AND Password = @Password AND IsWorking = 1";
                var parameters = new { Email = userName, Password = password };
                var user = await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, parameters);
                return user;
            }
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using (var connection = GetConnection())
            {
                var sql = @"UPDATE Employees SET Password = @Password WHERE Email = @Email";
                var parameters = new { Email = userName, Password = password };
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
        }
    }
}
