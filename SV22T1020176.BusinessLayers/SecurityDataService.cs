using SV22T1020176.DataLayers.Interfaces;
using SV22T1020176.DataLayers.SqlServer;
using SV22T1020176.Models.Security;

namespace SV22T1020176.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng nghiệp vụ liên quan đến bảo mật (Đăng nhập, Đổi mật khẩu...)
    /// </summary>
    public static class SecurityDataService
    {
        private static readonly IUserAccountRepository userAccountDB;
        private static readonly IUserAccountRepository customerAccountDB;

        /// <summary>
        /// Constructor tĩnh
        /// </summary>
        static SecurityDataService()
        {
            string connectionString = Configuration.ConnectionString;
            userAccountDB = new UserAccountRepository(connectionString);
            customerAccountDB = new CustomerAccountRepository(connectionString);
        }

        /// <summary>
        /// Kiểm tra đăng nhập
        /// </summary>
        /// <param name="userName">Tên đăng nhập</param>
        /// <param name="password">Mật khẩu (plain text sẽ được hash tự động)</param>
        /// <param name="userType">Loại người dùng (Nhân viên / Khách hàng)</param>
        /// <returns>Thông tin tài khoản nếu hợp lệ; ngược lại trả về null</returns>
        public static async Task<UserAccount?> AuthorizeAsync(string userName, string password, UserTypes userType)
        {
            string hashedPwd = CryptHelper.HashMD5(password);
            if (userType == UserTypes.Employee)
                return await userAccountDB.AuthorizeAsync(userName, hashedPwd);
            else
                return await customerAccountDB.AuthorizeAsync(userName, hashedPwd);
        }

        /// <summary>
        /// Thực hiện đổi mật khẩu
        /// </summary>
        /// <param name="userName">Tên tài khoản</param>
        /// <param name="password">Mật khẩu mới (plain text sẽ được hash tự động)</param>
        /// <param name="userType">Loại người dùng</param>
        /// <returns>True nếu thành công</returns>
        public static async Task<bool> ChangePasswordAsync(string userName, string password, UserTypes userType)
        {
            string hashedPwd = CryptHelper.HashMD5(password);
            if (userType == UserTypes.Employee)
                return await userAccountDB.ChangePasswordAsync(userName, hashedPwd);
            else
                return await customerAccountDB.ChangePasswordAsync(userName, hashedPwd);
        }
    }
}
