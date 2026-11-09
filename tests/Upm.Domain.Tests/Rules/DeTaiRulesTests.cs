using Upm.Domain.Enums;
using Upm.Domain.Rules;

namespace Upm.Domain.Tests.Rules;

/// <summary>Luật đề tài & phân công B01–B11 (đặc tả mục 5). Tên test theo mã luật (CLAUDE.md §6).</summary>
public sealed class DeTaiRulesTests
{
    [Fact]
    public void B01_De_tai_moi_mac_dinh_1_SV()
    {
        var kq = DeTaiRules.KiemTraSoSV(LoaiDeTai.LyThuyet, 1);

        Assert.True(kq.Dat);
        Assert.Equal("B01", kq.MaNgoaiLe);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void B01_So_SV_ngoai_1_den_3_bi_tu_choi(int maxSV)
    {
        var kq = DeTaiRules.KiemTraSoSV(LoaiDeTai.CheTao, maxSV);

        Assert.False(kq.Dat);
        Assert.Equal("B01", kq.MaNgoaiLe);
    }

    [Theory]
    [InlineData(LoaiDeTai.CheTao)]
    [InlineData(LoaiDeTai.HeThong)]
    public void B02_Nhom_2_SV_chi_cho_de_tai_che_tao_hoac_he_thong(LoaiDeTai loai)
    {
        var kq = DeTaiRules.KiemTraSoSV(loai, 2);

        Assert.True(kq.Dat);
        Assert.Equal("B02", kq.MaNgoaiLe);
        Assert.Contains("Bộ môn duyệt", kq.LyDo);
    }

    [Fact]
    public void B02_De_tai_ly_thuyet_khong_nhan_nhom()
    {
        var kq = DeTaiRules.KiemTraSoSV(LoaiDeTai.LyThuyet, 3);

        Assert.False(kq.Dat);
        Assert.Equal("B02", kq.MaNgoaiLe);
    }

    [Fact]
    public void B03_GV_con_quota_duoc_nhan_them()
    {
        var kq = DeTaiRules.KiemTraQuota(dangCo: 4, quota: 5, coDuyetNgoaiLe: false);

        Assert.True(kq.Dat);
        Assert.Equal("B03", kq.MaNgoaiLe);
    }

    [Fact]
    public void B03_GV_het_quota_bi_chan_neu_chua_co_duyet_ngoai_le()
    {
        var kq = DeTaiRules.KiemTraQuota(dangCo: 5, quota: 5, coDuyetNgoaiLe: false);

        Assert.False(kq.Dat);
        Assert.Equal("B03", kq.MaNgoaiLe);
    }

    [Fact]
    public void B03_Vuot_quota_co_duyet_ngoai_le_thi_duoc_nhan_nhung_van_ghi_ma()
    {
        var kq = DeTaiRules.KiemTraQuota(dangCo: 5, quota: 5, coDuyetNgoaiLe: true);

        Assert.True(kq.Dat);
        Assert.Equal("B03", kq.MaNgoaiLe);
        Assert.Contains("ghi log", kq.LyDo);
    }

    [Fact]
    public void B05_Goi_y_chon_GV_co_ti_le_tai_thap_nhat_con_cho()
    {
        var gv = new[]
        {
            new TaiGiangVien(GiangVienId: 1, DangCo: 4, Quota: 5), // 80%
            new TaiGiangVien(GiangVienId: 2, DangCo: 1, Quota: 8), // 12.5%
            new TaiGiangVien(GiangVienId: 3, DangCo: 5, Quota: 5), // đã đủ, không gợi ý
            new TaiGiangVien(GiangVienId: 4, DangCo: 1, Quota: 0), // quota 0: không gợi ý
        };

        Assert.Equal(2, DeTaiRules.GoiYGiangVien(gv));
    }

    [Fact]
    public void B05_Khong_co_GV_con_cho_thi_khong_goi_y()
    {
        Assert.Null(DeTaiRules.GoiYGiangVien([new TaiGiangVien(1, 5, 5)]));
    }

    [Fact]
    public void B06_GVDD_trung_GVHD_bi_chan_cung()
    {
        var kq = DeTaiRules.KiemTraTrungVaiTro(gvhdId: 7, gvddId: 7, dongHdIds: []);

        Assert.False(kq.Dat);
        Assert.Equal("B06", kq.MaNgoaiLe);
    }

    [Fact]
    public void B06_Dong_HD_trung_vai_tro_khac_cung_bi_chan()
    {
        Assert.False(DeTaiRules.KiemTraTrungVaiTro(7, 8, [8]).Dat);
        Assert.True(DeTaiRules.KiemTraTrungVaiTro(7, 8, [9]).Dat);
    }

    [Fact]
    public void B07_GV_da_khai_bao_xung_dot_bi_chan()
    {
        var kq = DeTaiRules.KiemTraXungDot([1, 2], new HashSet<long> { 2 });

        Assert.False(kq.Dat);
        Assert.Equal("B07", kq.MaNgoaiLe);
    }

    [Fact]
    public void B07_Khong_khai_bao_thi_cho_phan_cong()
    {
        Assert.True(DeTaiRules.KiemTraXungDot([1, 2], new HashSet<long>()).Dat);
    }

    [Fact]
    public void B08_Doi_GVHD_la_cung_luat_phan_cong_nen_GV_moi_phai_qua_B06_B07_B11()
    {
        // Đổi GVHD dùng cùng hàm kiểm tra với phân công: GVHD mới không được trùng GVĐD hiện có.
        Assert.False(DeTaiRules.KiemTraTrungVaiTro(gvhdId: 5, gvddId: 5, dongHdIds: []).Dat);
    }

    [Fact]
    public void B09_Doi_ten_trong_han_duoc_phep()
    {
        var kq = DeTaiRules.KiemTraHanDoiDeTai(homNay: new DateOnly(2026, 10, 1), hanDoi: new DateOnly(2026, 10, 4));

        Assert.True(kq.Dat);
        Assert.Equal("B09", kq.MaNgoaiLe);
    }

    [Fact]
    public void B09_Qua_han_doi_ten_bi_tu_choi()
    {
        var kq = DeTaiRules.KiemTraHanDoiDeTai(homNay: new DateOnly(2026, 11, 9), hanDoi: new DateOnly(2026, 10, 4));

        Assert.False(kq.Dat);
        Assert.Equal("B09", kq.MaNgoaiLe);
    }

    [Fact]
    public void B10_Ten_trung_sau_khi_bo_dau_duoc_canh_bao()
    {
        var cu = new[] { (Id: 1L, Ten: "Thiết kế hệ thống quản lý kho hàng", MoTa: (string?)null) };

        var trung = DeTaiRules.TimTrung("Thiet ke he thong quan ly kho hang", null, cu);

        var item = Assert.Single(trung);
        Assert.Equal(1, item.Id);
        Assert.True(item.TrungTen);
        Assert.Equal(1d, item.DoTuongDong);
    }

    [Fact]
    public void B10_Ten_khac_han_khong_bi_canh_bao()
    {
        var cu = new[] { (Id: 1L, Ten: "Nghiên cứu cầu dây văng", MoTa: (string?)null) };

        Assert.Empty(DeTaiRules.TimTrung("Ứng dụng AI trong quản lý điểm", null, cu));
    }

    [Fact]
    public void B10_Mo_ta_trung_ten_gan_giong_cung_bi_canh_bao()
    {
        var mo = "Xây dựng website tra cứu lịch thi cho sinh viên trường đại học";
        var cu = new[] { (Id: 2L, Ten: "Website tra cứu lịch thi", MoTa: (string?)mo) };

        var trung = DeTaiRules.TimTrung("Website tra cứu lịch thi sinh viên", mo, cu);

        var item = Assert.Single(trung);
        Assert.False(item.TrungTen);
        Assert.True(item.DoTuongDong >= DeTaiRules.NguongTrung);
    }

    [Fact]
    public void B10_Chuan_hoa_bo_dau_va_dd()
    {
        Assert.Equal("duong dan bien", DeTaiRules.ChuanHoa("  Đường dẫn, biển!  "));
    }

    [Fact]
    public void B11_GV_ngoai_truong_khong_co_co_huu_bi_chan()
    {
        Assert.False(DeTaiRules.KiemTraGVNgoaiTruong(coGVNgoaiTruong: true, coGVCoHuu: false).Dat);
        Assert.True(DeTaiRules.KiemTraGVNgoaiTruong(coGVNgoaiTruong: true, coGVCoHuu: true).Dat);
        Assert.True(DeTaiRules.KiemTraGVNgoaiTruong(coGVNgoaiTruong: false, coGVCoHuu: false).Dat);
    }

    [Theory]
    [InlineData(0, 5, MucTaiGiangVien.Thap)]
    [InlineData(4, 5, MucTaiGiangVien.Cao)]
    [InlineData(5, 5, MucTaiGiangVien.DuQuota)]
    [InlineData(6, 5, MucTaiGiangVien.VuotQuota)]
    public void Muc_tai_hien_thi_theo_quota(int dangCo, int quota, MucTaiGiangVien muc) =>
        Assert.Equal(muc, DeTaiRules.MucTai(dangCo, quota));
}
