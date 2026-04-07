using System.Security.Cryptography;
using System.Text;

namespace SV22T1020176.BusinessLayers
{
    /// <summary>
    /// Helper mã hóa MD5 dùng chung cho toàn bộ hệ thống (Admin + Shop)
    /// </summary>
    public static class CryptHelper
    {
        /// <summary>
        /// Băm chuỗi thành MD5 hash (32 ký tự hex, viết thường)
        /// </summary>
        public static string HashMD5(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// So sánh chuỗi plain text với chuỗi MD5 hash (không phân biệt hoa thường)
        /// </summary>
        public static bool VerifyMD5(string plainText, string md5Hash)
        {
            return string.Equals(HashMD5(plainText), md5Hash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
