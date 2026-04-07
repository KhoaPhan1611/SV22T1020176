using SV22T1020176.Models.HR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020176.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu trên Employee
    /// </summary>
    public interface IEmployeeRepository : IGenericRepository<Employee>
    {
        /// <summary>
        /// Kiểm tra xem email của nhân viên có hợp lệ không
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: Kiểm tra email của nhân viên mới
        /// Nếu id <> 0: Kiểm tra email của nhân viên có mã là id
        /// </param>
        /// <returns></returns>
        Task<bool> ValidateEmailAsync(string email, int id = 0);

        /// <summary>
        /// Cập nhật mật khẩu của nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần đổi mật khẩu</param>
        /// <param name="password">Mật khẩu mới</param>
        /// <returns>True nếu cập nhật thành công, ngược lại False</returns>
        Task<bool> ChangePasswordAsync(int id, string password);

        /// <summary>
        /// Cập nhật quyền của nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <param name="roleNames">Danh sách quyền (phân cách bởi dấu phẩy)</param>
        /// <returns>True nếu cập nhật thành công</returns>
        Task<bool> UpdateRoleNamesAsync(int id, string roleNames);
    }
}
