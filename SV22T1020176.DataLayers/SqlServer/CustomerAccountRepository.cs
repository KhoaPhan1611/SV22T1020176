using Dapper;
using SV22T1020176.DataLayers.Interfaces;
using SV22T1020176.Models.Security;

namespace SV22T1020176.DataLayers.SqlServer
{
    /// <summary>
    /// Xử lý tài khoản khách hàng đối với SQL Server
    /// </summary>
    public class CustomerAccountRepository : BaseSqlDAL, IUserAccountRepository
    {
        public CustomerAccountRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using (var connection = GetConnection())
            {
                System.Diagnostics.Debug.WriteLine($"[CustomerAccountRepo] Authorize: userName={userName}, passwordHash={password}");
                // Password đã được hash MD5 bằng C# (CryptHelper) trước khi gọi đến đây.
                // So sánh trực tiếp: password (đã hash) = Password (đã hash) trong DB.
                var sql = @"SELECT CAST(CustomerID AS VARCHAR) AS UserID, Email, CustomerName AS FullName
                            FROM Customers
                            WHERE Email = @Email
                                AND Password = @Password
                                AND IsLocked = 0";
                var parameters = new { Email = userName, Password = password };
                return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, parameters);
            }
        }

        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using (var connection = GetConnection())
            {
                // Password đã hash MD5 bằng C#, lưu trực tiếp.
                var sql = @"UPDATE Customers
                            SET Password = @Password
                            WHERE Email = @Email";
                var parameters = new { Email = userName, Password = password };
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
        }
    }
}
