namespace Upm.Domain.PhanQuyen;

/// <summary>Mã vai trò hệ thống (bảng VaiTro.MaVaiTro) – khớp dữ liệu seed.</summary>
public static class VaiTroMa
{
    public const string QuanTri = nameof(QuanTri);
    public const string PhongDaoTao = nameof(PhongDaoTao);
    public const string LanhDaoKhoa = nameof(LanhDaoKhoa);
    public const string TruongBoMon = nameof(TruongBoMon);
    public const string GiaoVu = nameof(GiaoVu);
    public const string GiangVien = nameof(GiangVien);
    public const string SinhVien = nameof(SinhVien);
    public const string ThuVien = nameof(ThuVien);
    /// <summary>Vai trò theo từng hồ sơ, suy ra từ PhanCong; không phải vai trò tài khoản.</summary>
    public const string GVHD = nameof(GVHD);
    /// <summary>Vai trò theo từng hồ sơ, suy ra từ PhanCong; không phải vai trò tài khoản.</summary>
    public const string GVDD = nameof(GVDD);
    /// <summary>Vai trò theo từng hồ sơ, suy ra từ lịch bảo vệ.</summary>
    public const string ThanhVienHoiDong = nameof(ThanhVienHoiDong);
    /// <summary>Chỉ dùng cho tác vụ nội bộ, không cấp qua tài khoản đăng nhập.</summary>
    public const string HeThong = nameof(HeThong);

    public static readonly IReadOnlyList<string> TatCa =
        [QuanTri, PhongDaoTao, LanhDaoKhoa, TruongBoMon, GiaoVu, GiangVien, SinhVien, ThuVien];
}
