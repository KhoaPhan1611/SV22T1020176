using System.Security.Cryptography;
using System.Text;

namespace SV22T1020176.Admin;

/// <summary>
/// Cung cấp các công cụ hỗ trợ xử lý bảo mật và băm dữ liệu cho toàn bộ hệ thống.
/// </summary>
public static class CryptHelper
{
    /// <summary>
    /// Thực hiện băm chuỗi đầu vào sang định dạng MD5 (dùng cho mật khẩu hoặc chữ ký số).
    /// </summary>
    /// <param name="plainText">Chuỗi văn bản gốc cần xử lý.</param>
    /// <returns>Chuỗi lục phân đại diện cho giá trị băm.</returns>
    public static string HashMD5(string plainText)
    {
        using var md5Provider = MD5.Create();
        var rawData = Encoding.UTF8.GetBytes(plainText);
        var computedHash = md5Provider.ComputeHash(rawData);

        var hexBuilder = new StringBuilder();
        foreach (var b in computedHash)
        {
            hexBuilder.Append(b.ToString("x2"));
        }
        return hexBuilder.ToString();
    }
}