namespace Upm.Infrastructure.Identity;

/// <summary>Cấu hình mục "XacThuc" trong appsettings.</summary>
public sealed class XacThucOptions
{
    public const string SectionName = "XacThuc";

    /// <summary>
    /// Giới hạn số request đăng nhập / IP / phút (chống dò hàng loạt). Đặt rộng (300) vì nhiều SV dùng chung
    /// IP qua NAT ký túc xá / phòng máy; chặn dò mật khẩu từng tài khoản do <see cref="DangNhapSai"/> đảm nhiệm.
    /// </summary>
    public int SoLanDangNhapMoiPhut { get; set; } = 300;

    public DangNhapSaiOptions DangNhapSai { get; set; } = new();

    /// <summary>
    /// Origin được phép gọi /auth/refresh và /auth/logout (chống CSRF). Rỗng → dùng Cors:AllowedOrigins.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];
}

/// <summary>Khóa tạm cặp (TenDangNhap, IP) sau N lần sai trong cửa sổ thời gian.</summary>
public sealed class DangNhapSaiOptions
{
    /// <summary>Số lần sai tối đa trong cửa sổ.</summary>
    public int SoLanToiDa { get; set; } = 5;

    /// <summary>Cửa sổ đếm lần sai và thời gian khóa (phút).</summary>
    public int ThoiGianKhoaPhut { get; set; } = 15;
}
