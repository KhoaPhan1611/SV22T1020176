namespace SV22T1020176.BusinessLayers;

/// <summary>
/// Khởi tạo và lưu trữ các giá trị cấu hình cho BusinessLayer
/// </summary>
public static class Configuration
{
    private static string connectionString = "";

    /// <summary>
    /// Khởi tạo cấu hình
    /// </summary>
    /// <param name="connectionString"></param>
    public static void Initialize(string connectionString)
    {
        Configuration.connectionString = connectionString;
    }

    /// <summary>
    /// Chuỗi kết nối đến cơ sở dữ liệu
    /// </summary>
    public static string ConnectionString
    {
        get
        {
            return connectionString;
        }
    }
}
