namespace Upm.Application.Abstractions;

/// <summary>
/// Người dùng đang thao tác – chỉ đọc từ token/HTTP request (KHÔNG truy vấn DB, dùng được trong interceptor EF).
/// Vai trò + phạm vi nạp từ DB qua <see cref="PhanQuyen.IQuyenHienTai"/>.
/// </summary>
public interface ICurrentUser
{
    bool DaDangNhap { get; }

    /// <summary>TaiKhoan.Id; null = chưa đăng nhập / hệ thống / job nền.</summary>
    long? TaiKhoanId { get; }

    string? TenDangNhap { get; }

    /// <summary>Tài khoản gắn với giảng viên (TaiKhoan.GiangVienId).</summary>
    long? GiangVienId { get; }

    /// <summary>Tài khoản gắn với sinh viên (TaiKhoan.SinhVienId).</summary>
    long? SinhVienId { get; }

    /// <summary>Cơ sở đang làm việc do client chọn (header X-CoSo-Id); hợp lệ hay không do IQuyenHienTai kiểm.</summary>
    long? CoSoDangChonId { get; }

    string? IpAddress { get; }
}
