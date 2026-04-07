namespace SV22T1020176.Admin;

/// <summary>
/// Cấu trúc phản hồi đồng nhất cho các yêu cầu AJAX hoặc API cục bộ.
/// </summary>
public class ApiResult
{
    public ApiResult(int status, string info = "")
    {
        Code = status;
        Message = info;
    }

    /// <summary>
    /// Mã trạng thái (0: Thất bại/Lỗi, 1+: Thành công/Xử lý ổn).
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// Nội dung thông điệp chi tiết trả về từ máy chủ.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}