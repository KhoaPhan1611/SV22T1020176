namespace SV22T1020176.Models.Security
{
    /// <summary>
    /// Tài khoản người dùng
    /// </summary>
    public class UserAccount
    {
        /// <summary>
        /// Tên đăng nhập
        /// </summary>
        public string UserID { get; set; } = "";
        /// <summary>
        /// Tên hiển thị
        /// </summary>
        public string FullName { get; set; } = "";
        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; } = "";
        /// <summary>
        /// Ảnh đại diện
        /// </summary>
        public string? Photo { get; set; }
    }
}