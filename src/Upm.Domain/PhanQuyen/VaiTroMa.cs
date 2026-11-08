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

    public static readonly IReadOnlyList<string> TatCa =
        [QuanTri, PhongDaoTao, LanhDaoKhoa, TruongBoMon, GiaoVu, GiangVien, SinhVien, ThuVien];
}
