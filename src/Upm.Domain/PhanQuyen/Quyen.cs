namespace Upm.Domain.PhanQuyen;

/// <summary>
/// Danh mục quyền chức năng (permission). Backend khai báo bằng <c>[CoQuyen(Quyen.X)]</c>,
/// frontend ẩn/hiện bằng <c>*coQuyen</c>. Vai trò → quyền ánh xạ trong <see cref="BangQuyen"/>
/// (theo mục "Actor" của dac-ta-nghiep-vu.html). Phạm vi dữ liệu xử lý riêng bởi IScopeFilter.
/// </summary>
public static class Quyen
{
    // Hệ thống
    public const string TaiKhoanQuanLy = "TaiKhoan.QuanLy";
    public const string RuleSetQuanLy = "RuleSet.QuanLy";
    public const string DanhMucQuanLy = "DanhMuc.QuanLy";
    public const string AuditLogXem = "AuditLog.Xem";

    // Đợt & xét điều kiện (UC-1)
    public const string DotQuanLy = "Dot.QuanLy";
    public const string XetDieuKienChay = "XetDieuKien.Chay";
    public const string QuyetDinhGhiNhan = "QuyetDinh.GhiNhan";

    // Đề tài & phân công (UC-2)
    public const string DeTaiDeXuat = "DeTai.DeXuat";
    public const string DeTaiDuyet = "DeTai.Duyet";
    public const string PhanCongQuanLy = "PhanCong.QuanLy";

    // Hội đồng & bảo vệ (UC-4)
    public const string HoiDongDeXuat = "HoiDong.DeXuat";
    public const string LichBaoVeXep = "LichBaoVe.Xep";

    // Điểm (UC-5)
    public const string DiemChot = "Diem.Chot";
    public const string DiemSuaSauChot = "Diem.SuaSauChot";

    // Tra cứu / chung
    public const string SinhVienXem = "SinhVien.Xem";
    public const string BaoCaoXem = "BaoCao.Xem";
    public const string HoSoCuaToi = "HoSo.CuaToi";
    public const string ThuVienTiepNhan = "ThuVien.TiepNhan";
}

public static class BangQuyen
{
    private static readonly Dictionary<string, string[]> TheoVaiTro = new(StringComparer.Ordinal)
    {
        [VaiTroMa.QuanTri] =
        [
            Quyen.TaiKhoanQuanLy, Quyen.RuleSetQuanLy, Quyen.DanhMucQuanLy, Quyen.AuditLogXem, Quyen.SinhVienXem,
        ],
        [VaiTroMa.PhongDaoTao] =
        [
            Quyen.DotQuanLy, Quyen.XetDieuKienChay, Quyen.QuyetDinhGhiNhan, Quyen.DiemChot, Quyen.DiemSuaSauChot,
            Quyen.SinhVienXem, Quyen.BaoCaoXem, Quyen.AuditLogXem,
        ],
        [VaiTroMa.LanhDaoKhoa] = [Quyen.DeTaiDuyet, Quyen.HoiDongDeXuat, Quyen.SinhVienXem, Quyen.BaoCaoXem],
        [VaiTroMa.TruongBoMon] = [Quyen.DeTaiDuyet, Quyen.PhanCongQuanLy, Quyen.SinhVienXem],
        [VaiTroMa.GiaoVu] = [Quyen.LichBaoVeXep, Quyen.SinhVienXem],
        [VaiTroMa.GiangVien] = [Quyen.DeTaiDeXuat, Quyen.SinhVienXem],
        [VaiTroMa.SinhVien] = [Quyen.DeTaiDeXuat, Quyen.HoSoCuaToi],
        [VaiTroMa.ThuVien] = [Quyen.ThuVienTiepNhan],
    };

    /// <summary>Hợp các quyền của những vai trò đã cho (mã vai trò lạ bị bỏ qua).</summary>
    public static IReadOnlySet<string> ChoVaiTro(IEnumerable<string> maVaiTro) =>
        maVaiTro.SelectMany(v => TheoVaiTro.GetValueOrDefault(v) ?? []).ToHashSet(StringComparer.Ordinal);

    /// <summary>Các vai trò có quyền <paramref name="quyen"/>.</summary>
    public static IReadOnlyList<string> VaiTroCoQuyen(string quyen) =>
        TheoVaiTro.Where(kv => kv.Value.Contains(quyen, StringComparer.Ordinal)).Select(kv => kv.Key).ToList();
}
