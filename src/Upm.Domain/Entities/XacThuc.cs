// [Bổ sung Phần 3 – không có trong thiet-ke.md] Bảng hỗ trợ xác thực.
using Upm.Domain.Common;

namespace Upm.Domain.Entities;

/// <summary>
/// Refresh token (xoay vòng). Chỉ lưu SHA-256 của token, không lưu token gốc.
/// Các token sinh ra từ cùng một lần đăng nhập chung <see cref="HoToken"/>: nếu một token ĐÃ thu hồi
/// bị dùng lại (dấu hiệu bị đánh cắp) thì thu hồi cả họ. Không có cột audit.
/// </summary>
public class RefreshToken : Entity
{
    public long TaiKhoanId { get; set; }
    public TaiKhoan TaiKhoan { get; set; } = null!;
    /// <summary>SHA-256 hex (64 ký tự) của token.</summary>
    public string TokenHash { get; set; } = string.Empty;
    public Guid HoToken { get; set; }
    public DateTime TaoLuc { get; set; }
    public DateTime HetHanLuc { get; set; }
    public DateTime? ThuHoiLuc { get; set; }
    public string? LyDoThuHoi { get; set; }
    /// <summary>Token mới thay thế (khi xoay vòng).</summary>
    public long? ThayTheBoiId { get; set; }
    public RefreshToken? ThayTheBoi { get; set; }
    public string? TaoTuIp { get; set; }

    public bool ConHieuLuc(DateTime nowUtc) => ThuHoiLuc is null && HetHanLuc > nowUtc;
}
