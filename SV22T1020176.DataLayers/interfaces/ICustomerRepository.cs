using SV22T1020176.Models.Partner;

namespace SV22T1020176.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu trên Customer
    /// </summary>
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        /// <summary>
        /// Kiểm tra xem một địa chỉ email có hợp lệ hay không?
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: Kiểm tra email của khách hàng mới.
        /// Nếu id <> 0: Kiểm tra email đối với khách hàng đã tồn tại
        /// </param>
        /// <returns></returns>
        Task<bool> ValidateEmailAsync(string email, int id = 0);
        /// <summary>
        /// Thay đổi mật khẩu của khách hàng
        /// </summary>
        /// <param name="id"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        Task<bool> ChangePasswordAsync(int id, string password);
    }
}
