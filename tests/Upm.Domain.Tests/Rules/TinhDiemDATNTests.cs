using Upm.Domain.Common;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Domain.Rules;

namespace Upm.Domain.Tests.Rules;

/// <summary>Công thức điểm (mục 8), T11–T15, F02–F04, E01, E02, E08, E10 (⚠O9).</summary>
public class TinhDiemDATNTests
{
    private static ThamSoTinhDiem TrongSo(decimal qt, decimal hd, bool tach = false) => new(qt, hd, tach, 5.5m);

    // ---------- Test biên mục 11 ----------

    [Fact]
    public void T11_Diem_DATN_5_4_D_cong_KHONG_DAT()
    {
        // QT 5,4; BV 5,4 → 5,4 với mọi trọng số.
        var kq = TinhDiemDATN.Tinh(new DiemThanhPhan(5.4m, 5.4m), HoiDongMau.Phieu(5.4m, 5.4m, 5.4m), TrongSo(0.3m, 0.7m));

        Assert.Equal(5.4m, kq.DiemDATN);
        Assert.Equal("D+", kq.DiemChu);
        Assert.False(kq.Dat);
    }

    [Fact]
    public void T12_QT_7_HD_5_trong_so_30_70_bang_5_6_dat_C()
    {
        var kq = TinhDiemDATN.Tinh(new DiemThanhPhan(7.0m, 7.0m), HoiDongMau.Phieu(5.0m, 5.0m, 5.0m), TrongSo(0.3m, 0.7m));

        Assert.Equal(7.0m, kq.DiemQuaTrinh);
        Assert.Equal(5.0m, kq.DiemBaoVe);
        Assert.Equal(5.6m, kq.DiemDATN);
        Assert.Equal("C", kq.DiemChu);
        Assert.Equal(2.0m, kq.DiemHe4);
        Assert.True(kq.Dat);
        Assert.Null(kq.MaNgoaiLe);
    }

    [Fact]
    public void T13_QT_7_HD_5_trong_so_40_60_bang_5_8_dat_khac_T12()
    {
        var kq = TinhDiemDATN.Tinh(new DiemThanhPhan(7.0m, 7.0m), HoiDongMau.Phieu(5.0m, 5.0m, 5.0m), TrongSo(0.4m, 0.6m));

        Assert.Equal(5.8m, kq.DiemDATN);
        Assert.True(kq.Dat);
    }

    [Fact]
    public void T14_HD_5_nguoi_co_mat_CT_TK_1_UV_khong_du_nguoi_chan_mo_phien()
    {
        var hd = HoiDong(HinhThucBaoVe.TrucTiep, null,
            Tv(1, ChucDanhHoiDong.ChuTich), Tv(2, ChucDanhHoiDong.ThuKy), Tv(3, ChucDanhHoiDong.UyVien),
            Tv(4, ChucDanhHoiDong.UyVien, coMat: false), Tv(5, ChucDanhHoiDong.UyVien, coMat: false));

        var kq = TinhDiemDATN.KiemTraMoPhien(hd, RuleSetMau.Utc2021);

        Assert.False(kq.Dat);
        Assert.Equal("E02", kq.MaNgoaiLe);
        Assert.Equal(MaLuat.MoPhien, kq.MaLuat);
        Assert.Contains("2 ủy viên", kq.LyDo, StringComparison.Ordinal);
    }

    [Fact]
    public void T15_HD_3_nguoi_truc_tuyen_1_nguoi_chua_dong_y_chan_chot_diem()
    {
        var hd = HoiDong(HinhThucBaoVe.TrucTuyen, "https://rec.utc.edu.vn/hd01",
            Tv(1, ChucDanhHoiDong.ChuTich, diem: 7m, dongY: true), Tv(2, ChucDanhHoiDong.ThuKy, diem: 7m, dongY: true),
            Tv(3, ChucDanhHoiDong.UyVien, diem: 7m, dongY: null));

        var kq = TinhDiemDATN.KiemTraChotDiem(hd, RuleSetMau.Utc2021, TrongSo(0.3m, 0.7m), false);

        Assert.False(kq.Dat);
        Assert.Equal("E08", kq.MaNgoaiLe);
        Assert.Contains("1 thành viên chưa đồng ý", kq.LyDo, StringComparison.Ordinal);
    }

    // ---------- Ngoại lệ ----------

    [Fact]
    public void F02_Tach_diem_khac_QT_bang_TBC_3_phan_moi_phan_10_phan_tram()
    {
        var kq = TinhDiemDATN.Tinh(new DiemThanhPhan(8.0m, 7.0m, 6.5m), HoiDongMau.Phieu(7.0m, 7.0m, 7.0m), TrongSo(0.3m, 0.7m, tach: true));

        Assert.Equal(7.2m, kq.DiemQuaTrinh); // (8 + 7 + 6,5) / 3 = 7,1666… → 7,2
        Assert.Equal(7.1m, kq.DiemDATN); // 7,2 × 0,3 + 7,0 × 0,7 = 7,06 → 7,1
    }

    [Fact]
    public void F02_Khong_tach_diem_khac_bo_qua_diem_khac()
    {
        var kq = TinhDiemDATN.Tinh(new DiemThanhPhan(8.0m, 7.0m, 1.0m), HoiDongMau.Phieu(7.0m, 7.0m, 7.0m), TrongSo(0.3m, 0.7m));

        Assert.Equal(7.5m, kq.DiemQuaTrinh);
    }

    [Fact]
    public void F02_Tach_diem_khac_nhung_thieu_diem_khac_bi_tu_choi()
    {
        var ex = Assert.Throws<DomainException>(() =>
            TinhDiemDATN.Tinh(new DiemThanhPhan(8.0m, 7.0m), HoiDongMau.Phieu(7m, 7m, 7m), TrongSo(0.3m, 0.7m, tach: true)));
        Assert.Equal("UPM_F02_THIEU_DIEM_KHAC", ex.Code);
    }

    [Theory]
    [InlineData(4.0, "D", false, "F03")]
    [InlineData(5.4, "D+", false, "F03")]
    [InlineData(5.5, "C", true, null)]
    [InlineData(3.9, "F+", false, null)]
    public void F03_Diem_4_0_den_5_4_KHONG_DAT_voi_DATN(decimal diem, string chu, bool dat, string? ma)
    {
        var kq = TinhDiemDATN.Tinh(new DiemThanhPhan(diem, diem), HoiDongMau.Phieu(diem, diem, diem), TrongSo(0.3m, 0.7m));

        Assert.Equal(chu, kq.DiemChu);
        Assert.Equal(dat, kq.Dat);
        Assert.Equal(ma, kq.MaNgoaiLe);
        Assert.Contains(dat ? "ĐẠT." : "KHÔNG ĐẠT", kq.LyDo, StringComparison.Ordinal);
    }

    [Fact]
    public void F04_Thieu_phieu_thanh_vien_co_mat_khong_tinh_va_khong_chot()
    {
        var hd = HoiDong(HinhThucBaoVe.TrucTiep, null,
            Tv(1, ChucDanhHoiDong.ChuTich, diem: 7m), Tv(2, ChucDanhHoiDong.ThuKy, diem: 7m), Tv(3, ChucDanhHoiDong.UyVien));

        var ex = Assert.Throws<DomainException>(() => TinhDiemDATN.Tinh(new DiemThanhPhan(7m, 7m), hd.ThanhVien, TrongSo(0.3m, 0.7m)));
        Assert.Equal("UPM_F04_THIEU_PHIEU_DIEM", ex.Code);

        var chot = TinhDiemDATN.KiemTraChotDiem(hd, RuleSetMau.Utc2021, TrongSo(0.3m, 0.7m), false);
        Assert.False(chot.Dat);
        Assert.Equal("F04", chot.MaNgoaiLe);
    }

    [Fact]
    public void Diem_bao_ve_chi_tinh_thanh_vien_co_mat()
    {
        IReadOnlyList<ThanhVienChamDiem> tv =
        [
            Tv(1, ChucDanhHoiDong.ChuTich, diem: 8m), Tv(2, ChucDanhHoiDong.ThuKy, diem: 7m),
            Tv(3, ChucDanhHoiDong.UyVien, diem: 6m), Tv(4, ChucDanhHoiDong.UyVien, coMat: false, diem: 1m),
        ];

        var kq = TinhDiemDATN.Tinh(new DiemThanhPhan(7m, 7m), tv, TrongSo(0.3m, 0.7m));

        Assert.Equal(7.0m, kq.DiemBaoVe);
    }

    [Fact]
    public void Khong_co_thanh_vien_co_mat_bi_tu_choi()
    {
        var ex = Assert.Throws<DomainException>(() =>
            TinhDiemDATN.Tinh(new DiemThanhPhan(7m, 7m), [Tv(1, ChucDanhHoiDong.ChuTich, coMat: false, diem: 7m)], TrongSo(0.3m, 0.7m)));
        Assert.Equal("UPM_E02_KHONG_CO_THANH_VIEN", ex.Code);
    }

    [Fact]
    public void Diem_ngoai_thang_10_bi_tu_choi()
    {
        var ex = Assert.Throws<DomainException>(() =>
            TinhDiemDATN.Tinh(new DiemThanhPhan(11m, 7m), HoiDongMau.Phieu(7m, 7m, 7m), TrongSo(0.3m, 0.7m)));
        Assert.Equal("UPM_DIEM_NGOAI_THANG_10", ex.Code);
    }

    [Theory]
    [InlineData(8.0, 6.0, true)] // chênh đúng 2,0 → cảnh báo
    [InlineData(8.0, 6.1, false)]
    public void O9_E10_Chenh_lech_tu_2_diem_canh_bao_va_can_Chu_tich_xac_nhan(decimal cao, decimal thap, bool canhBao)
    {
        var tv = HoiDongMau.Phieu(cao, 7.0m, thap);
        var kq = TinhDiemDATN.Tinh(new DiemThanhPhan(7m, 7m), tv, TrongSo(0.3m, 0.7m));

        Assert.Equal(canhBao, kq.CanhBaoChenhLech);
        Assert.Equal(cao - thap, kq.ChenhLechMax);
        Assert.Equal(canhBao ? "E10" : null, kq.MaNgoaiLe);
        Assert.Equal(canhBao, kq.LyDo.Contains("Chủ tịch xác nhận", StringComparison.Ordinal));

        var hd = new HoiDongChamDiem(HinhThucBaoVe.TrucTiep, null, tv);
        Assert.Equal(!canhBao, TinhDiemDATN.KiemTraChotDiem(hd, RuleSetMau.Utc2021, TrongSo(0.3m, 0.7m), false).Dat);
        Assert.True(TinhDiemDATN.KiemTraChotDiem(hd, RuleSetMau.Utc2021, TrongSo(0.3m, 0.7m), true).Dat);
    }

    [Fact]
    public void O9_Nguong_chenh_lech_lay_tu_tham_so()
    {
        var kq = TinhDiemDATN.Tinh(new DiemThanhPhan(7m, 7m), HoiDongMau.Phieu(8m, 7m, 6.5m), new ThamSoTinhDiem(0.3m, 0.7m, false, 5.5m, 1.5m));

        Assert.True(kq.CanhBaoChenhLech);
    }

    // ---------- Mở phiên / chốt ----------

    [Theory]
    [InlineData(3, 1)]
    [InlineData(4, 1)]
    [InlineData(5, 2)]
    public void So_uy_vien_toi_thieu_theo_quy_mo_hoi_dong(int n, int uv)
    {
        Assert.Equal(uv, TinhDiemDATN.SoUyVienToiThieu(n));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    public void E01_Hoi_dong_ngoai_3_den_5_thanh_vien(int n)
    {
        var tv = Enumerable.Range(1, n)
            .Select(i => Tv(i, i == 1 ? ChucDanhHoiDong.ChuTich : i == 2 ? ChucDanhHoiDong.ThuKy : ChucDanhHoiDong.UyVien))
            .ToArray();

        var kq = TinhDiemDATN.KiemTraMoPhien(HoiDong(HinhThucBaoVe.TrucTiep, null, tv), RuleSetMau.Utc2021);

        Assert.False(kq.Dat);
        Assert.Equal("E01", kq.MaNgoaiLe);
    }

    [Theory]
    [InlineData(false, true, 2)]
    [InlineData(true, false, 2)]
    [InlineData(true, true, 2)]
    public void E02_Mo_phien_can_CT_TK_va_du_UV(bool ct, bool tk, int soUVCoMat)
    {
        var hd = HoiDong(HinhThucBaoVe.TrucTiep, null,
            Tv(1, ChucDanhHoiDong.ChuTich, ct), Tv(2, ChucDanhHoiDong.ThuKy, tk),
            Tv(3, ChucDanhHoiDong.UyVien, soUVCoMat >= 1), Tv(4, ChucDanhHoiDong.UyVien, soUVCoMat >= 2),
            Tv(5, ChucDanhHoiDong.UyVien, soUVCoMat >= 3));

        var kq = TinhDiemDATN.KiemTraMoPhien(hd, RuleSetMau.Utc2021);

        Assert.Equal(ct && tk, kq.Dat);
        Assert.Equal(ct && tk ? null : "E02", kq.MaNgoaiLe);
    }

    [Fact]
    public void E08_Truc_tuyen_hop_le_cho_phep_chot()
    {
        var hd = HoiDong(HinhThucBaoVe.TrucTuyen, "https://rec.utc.edu.vn/hd01",
            Tv(1, ChucDanhHoiDong.ChuTich, diem: 7m, dongY: true), Tv(2, ChucDanhHoiDong.ThuKy, diem: 7.5m, dongY: true),
            Tv(3, ChucDanhHoiDong.UyVien, diem: 6.5m, dongY: true));

        var kq = TinhDiemDATN.KiemTraChotDiem(hd, RuleSetMau.Utc2021, TrongSo(0.3m, 0.7m), false);

        Assert.True(kq.Dat);
        Assert.Contains(kq.CacLuat, l => l.MaLuat == MaLuat.TrucTuyen && l.Dat);
        Assert.Null(kq.MaNgoaiLe);
    }

    [Fact]
    public void E08_Truc_tuyen_thieu_link_va_duoi_3_nguoi_co_mat()
    {
        var hd = HoiDong(HinhThucBaoVe.TrucTuyen, " ",
            Tv(1, ChucDanhHoiDong.ChuTich, diem: 7m, dongY: true), Tv(2, ChucDanhHoiDong.ThuKy, diem: 7m, dongY: true),
            Tv(3, ChucDanhHoiDong.UyVien, coMat: false));

        var kq = TinhDiemDATN.KiemTraChotDiem(hd, RuleSetMau.Utc2021, TrongSo(0.3m, 0.7m), false);

        var tt = kq.CacLuat.Single(l => l.MaLuat == MaLuat.TrucTuyen);
        Assert.False(tt.Dat);
        Assert.Contains("chưa có link ghi hình", tt.LyDo, StringComparison.Ordinal);
        Assert.Contains("cần ≥ 3", tt.LyDo, StringComparison.Ordinal);
    }

    [Fact]
    public void Chot_diem_khi_chua_ai_nop_phieu_khong_loi_chenh_lech()
    {
        var hd = HoiDong(HinhThucBaoVe.TrucTiep, null,
            Tv(1, ChucDanhHoiDong.ChuTich), Tv(2, ChucDanhHoiDong.ThuKy), Tv(3, ChucDanhHoiDong.UyVien));

        var kq = TinhDiemDATN.KiemTraChotDiem(hd, RuleSetMau.Utc2021, TrongSo(0.3m, 0.7m), false);

        Assert.True(kq.CacLuat.Single(l => l.MaLuat == MaLuat.ChenhLech).Dat);
        Assert.Equal("F04", kq.MaNgoaiLe);
    }

    // ---------- Trọng số theo RuleSet / Bộ môn ----------

    [Fact]
    public void Cung_du_lieu_UTC_2021_va_UTC_2026_khac_trong_so_cho_ket_qua_khac()
    {
        var diem = new DiemThanhPhan(7.0m, 7.0m);
        var phieu = HoiDongMau.Phieu(5.0m, 5.0m, 5.0m);

        var kq2021 = TinhDiemDATN.Tinh(diem, phieu, ThamSoTinhDiem.Tu(RuleSetMau.Entity2021(trongSoQT: 0.40m), null));
        var kq2026 = TinhDiemDATN.Tinh(diem, phieu, ThamSoTinhDiem.Tu(RuleSetMau.Entity2026(trongSoQT: 0.30m), null));

        Assert.Equal(0.40m, kq2021.TrongSoQT);
        Assert.Equal(0.60m, kq2021.TrongSoHD);
        Assert.Equal(5.8m, kq2021.DiemDATN);
        Assert.Equal(0.30m, kq2026.TrongSoQT);
        Assert.Equal(0.70m, kq2026.TrongSoHD);
        Assert.Equal(5.6m, kq2026.DiemDATN);
    }

    [Fact]
    public void Tham_so_bo_mon_ghi_de_trong_so_RuleSet()
    {
        var bm = new ThamSoBoMon { TrongSoQT = 0.40m, TrongSoHD = 0.60m, TachDiemKhac = true, SoNgaySuaGapToiDa = 3 };

        var ts = ThamSoTinhDiem.Tu(RuleSetMau.Entity2021(trongSoQT: 0.30m), bm);

        Assert.Equal(0.40m, ts.TrongSoQT);
        Assert.Equal(0.60m, ts.TrongSoHD);
        Assert.True(ts.TachDiemKhac);
        Assert.Equal(5.5m, ts.DiemDatDATN);
        Assert.Equal(ThamSoTinhDiem.NguongChenhLechMacDinh, ts.NguongChenhLech);
    }

    [Theory]
    [InlineData(0.3, 0.6)]
    [InlineData(-0.1, 1.1)]
    [InlineData(1.1, -0.1)]
    public void F01_Trong_so_khong_hop_le_bi_tu_choi(decimal qt, decimal hd)
    {
        var ex = Assert.Throws<DomainException>(() => new ThamSoTinhDiem(qt, hd, false, 5.5m));
        Assert.Equal("UPM_F01_TRONG_SO_KHONG_HOP_LE", ex.Code);
    }

    private static ThanhVienChamDiem Tv(long id, ChucDanhHoiDong chucDanh, bool coMat = true, decimal? diem = null, bool? dongY = null) =>
        new(id, chucDanh, coMat, diem, dongY);

    private static HoiDongChamDiem HoiDong(HinhThucBaoVe hinhThuc, string? link, params ThanhVienChamDiem[] tv) => new(hinhThuc, link, tv);
}
