using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;

namespace UniProject.API.Common
{
    /// <summary>
    /// Nhận diện lỗi do mất kết nối tới SQL Server (server tắt, sai tên server, mạng rớt, sai đăng nhập, DB không tồn tại...)
    /// để báo rõ "không kết nối được CSDL" thay vì lỗi 500 chung chung.
    /// </summary>
    public static class DatabaseErrors
    {
        // Mã lỗi SqlException liên quan tới kết nối, không phải lỗi dữ liệu/câu lệnh.
        private static readonly HashSet<int> ConnectionErrorNumbers =
        [
            -2,     // Timeout
            -1,     // Lỗi khi thiết lập kết nối
            2, 53,  // Không tìm thấy / không truy cập được server
            40, 64, 121, 233, 1231, 10053, 10054, 10060, 10061, 11001, // Lỗi mạng, kết nối bị đóng
            4060,   // Không mở được database
            18456,  // Đăng nhập thất bại
        ];

        public static bool IsConnectionFailure(Exception exception)
        {
            for (var ex = exception; ex != null; ex = ex.InnerException)
            {
                // EnableRetryOnFailure đã thử lại nhiều lần mà vẫn lỗi tạm thời → coi như mất kết nối.
                if (ex is RetryLimitExceededException)
                    return true;
                if (ex is SqlException sql && sql.Errors.Cast<SqlError>().Any(e => ConnectionErrorNumbers.Contains(e.Number)))
                    return true;
            }
            return false;
        }
    }
}
