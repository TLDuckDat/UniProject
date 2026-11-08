using Upm.Domain.Common;

namespace Upm.Application.XacThuc;

/// <summary>Lỗi xác thực → HTTP 401 (khác DomainException thường → 422).</summary>
public sealed class XacThucException(string code, string message) : DomainException(code, message);

/// <summary>Đăng nhập sai quá nhiều lần → tạm khóa (HTTP 429 + Retry-After).</summary>
public sealed class TamKhoaDangNhapException(string code, string message, TimeSpan thuLaiSau) : DomainException(code, message)
{
    public TimeSpan ThuLaiSau { get; } = thuLaiSau;
}

public sealed record DangNhapRequest(string TenDangNhap, string MatKhau);

/// <summary>Access token trả trong body; refresh token chỉ đi qua cookie HttpOnly (không có trong DTO).</summary>
public sealed record TokenResponse(string AccessToken, DateTimeOffset HetHanLuc, NguoiDungResponse NguoiDung);

public sealed record PhamViResponse(
    string MaVaiTro,
    string TenVaiTro,
    long? CoSoId,
    string? MaCoSo,
    long? KhoaId,
    string? TenKhoa,
    long? BoMonId,
    string? TenBoMon);

public sealed record CoSoResponse(long Id, string MaCoSo, string TenCoSo);

public sealed record NguoiDungResponse(
    long Id,
    string TenDangNhap,
    string? HoTen,
    string? Email,
    long? GiangVienId,
    long? SinhVienId,
    IReadOnlyList<PhamViResponse> VaiTro,
    IReadOnlyList<string> Quyen,
    IReadOnlyList<CoSoResponse> CoSoDuocPhep);

/// <summary>Kết quả cấp token nội bộ (kèm refresh token gốc để API ghi cookie).</summary>
public sealed record PhienDangNhap(TokenResponse Token, string RefreshToken, DateTimeOffset RefreshHetHanLuc);

public interface IXacThucService
{
    /// <summary>Ném DomainException UPM_AUTH_SAI_THONG_TIN nếu sai tên/mật khẩu hoặc tài khoản bị khóa.</summary>
    Task<PhienDangNhap> DangNhapAsync(DangNhapRequest request, CancellationToken ct = default);

    /// <summary>Xoay vòng refresh token. Ném DomainException UPM_AUTH_REFRESH_KHONG_HOP_LE nếu token sai/hết hạn/đã thu hồi.</summary>
    Task<PhienDangNhap> LamMoiAsync(string refreshToken, CancellationToken ct = default);

    Task DangXuatAsync(string? refreshToken, CancellationToken ct = default);

    Task<NguoiDungResponse> ThongTinAsync(long taiKhoanId, CancellationToken ct = default);
}
