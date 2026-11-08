using Upm.Domain.Common;
using Upm.Domain.Enums;
using Upm.Domain.Rules;

namespace Upm.Domain.Tests.Rules;

/// <summary>Xét bảo vệ K1–K6 + bảng quyết định (đặc tả mục 7), T08, T09, T16, T18, D01–D08.</summary>
public class XetBaoVeTests
{
    private static KetQuaXetBaoVe Xet(HoSoXetBaoVe hoSo, ThamSoQuyChe? rs = null) =>
        XetBaoVe.Xet(hoSo, rs ?? RuleSetMau.Utc2021, HocTap.KyXet, HoSo.NgayXet, LichHocKyGia.Instance);

    private static HocTap DuTC() => HocTap.DungTienDo().NenDaDat().Dat(5, LoaiHocPhan.TTTN).ChuaHoc(10, LoaiHocPhan.DATN);

    // ---------- Bảng quyết định ----------

    public static TheoryData<bool, bool, KetQuaDocDuyetLoai, bool, decimal, bool, KetLuanXetBaoVe> BangQuyetDinh => new()
    {
        // K1 ok, K2, K3,                     K4 ok, GPA,  K6 ok, kết luận
        { true, true, KetQuaDocDuyetLoai.DongY, true, 2.50m, true, KetLuanXetBaoVe.DUOC_BAO_VE },
        { false, true, KetQuaDocDuyetLoai.DongY, true, 2.50m, true, KetLuanXetBaoVe.PHAI_LAM_LAI },
        { false, false, KetQuaDocDuyetLoai.SuaGap, false, 1.00m, false, KetLuanXetBaoVe.PHAI_LAM_LAI },
        { true, false, KetQuaDocDuyetLoai.DongY, false, 2.50m, true, KetLuanXetBaoVe.PHAI_LAM_LAI },
        { true, true, KetQuaDocDuyetLoai.KhongDongY, false, 1.00m, true, KetLuanXetBaoVe.PHAI_LAM_LAI },
        { true, true, KetQuaDocDuyetLoai.SuaGap, false, 1.00m, false, KetLuanXetBaoVe.YEU_CAU_CHINH_SUA },
        { true, true, KetQuaDocDuyetLoai.DongY, false, 2.50m, true, KetLuanXetBaoVe.HOAN_BAO_VE },
        { true, true, KetQuaDocDuyetLoai.DongY, true, 1.80m, true, KetLuanXetBaoVe.HOAN_BAO_VE },
        { true, true, KetQuaDocDuyetLoai.DongY, true, 2.50m, false, KetLuanXetBaoVe.HOAN_BAO_VE },
    };

    [Theory]
    [MemberData(nameof(BangQuyetDinh))]
    public void Bang_quyet_dinh_muc_7(bool k1, bool k2, KetQuaDocDuyetLoai k3, bool k4, decimal gpa, bool k6, KetLuanXetBaoVe kyVong)
    {
        var hocTap = HocTap.DungTienDo().NenDaDat().ChuaHoc(10, LoaiHocPhan.DATN);
        hocTap = k6 ? hocTap.Dat(5, LoaiHocPhan.TTTN) : hocTap.ChuaHoc(5, LoaiHocPhan.TTTN);
        hocTap = k4 ? hocTap : hocTap.No(3);
        IReadOnlyList<KyLuat> kyLuat = k1 ? [] : [HoSo.DinhChi(new DateOnly(2026, 9, 1))];

        var kq = Xet(HoSo.BaoVe(hocTap, gpa, k2, k3, kyLuat));

        Assert.Equal(kyVong, kq.KetLuan);
        Assert.Equal([MaLuat.K1, MaLuat.K2, MaLuat.K3, MaLuat.K4, MaLuat.K5, MaLuat.K6], kq.CacLuat.Select(l => l.MaLuat));
        Assert.Equal(kyVong == KetLuanXetBaoVe.HOAN_BAO_VE, kq.NgayHanHoan is not null);
    }

    [Fact]
    public void Du_DK_bao_ve()
    {
        var kq = Xet(HoSo.BaoVe());

        Assert.True(kq.Dat);
        Assert.Null(kq.MaNgoaiLe);
        Assert.Equal(MaLuat.XetBaoVe, kq.MaLuat);
        Assert.Equal("UTC-2021", kq.RuleVersion);
        Assert.Contains("K1–K6", kq.LyDo, StringComparison.Ordinal);
        Assert.Null(kq.HocKyHanHoan);
    }

    // ---------- Test biên mục 11 ----------

    [Fact]
    public void T08_GPA_tho_1_895_lam_tron_1_90_K5_dat()
    {
        var kq = Xet(HoSo.BaoVe(gpa: 1.895m));

        Assert.Equal(1.90m, kq.GpaLamTron);
        Assert.True(kq[MaLuat.K5].Dat);
        Assert.Equal(KetLuanXetBaoVe.DUOC_BAO_VE, kq.KetLuan);
    }

    [Fact]
    public void T09_GPA_tho_1_8949_lam_tron_1_89_K5_truot_HOAN_BAO_VE()
    {
        var kq = Xet(HoSo.BaoVe(gpa: 1.8949m));

        Assert.Equal(1.89m, kq.GpaLamTron);
        Assert.False(kq[MaLuat.K5].Dat);
        Assert.Equal(KetLuanXetBaoVe.HOAN_BAO_VE, kq.KetLuan);
    }

    [Theory]
    [InlineData(4, TinhTrangSuaGap.GVDDPhaiQuyetDinh)] // T16: nộp sau 4 ngày, hạn 3
    [InlineData(3, TinhTrangSuaGap.DuocDocLai)]
    [InlineData(1, TinhTrangSuaGap.DuocDocLai)]
    public void T16_Nop_ban_sua_qua_han_GVDD_phai_quyet_khong_mo_vong_2(int nopSauSoNgay, TinhTrangSuaGap kyVong)
    {
        var yeuCau = new DateOnly(2026, 12, 1);

        var kq = XetBaoVe.KiemTraSuaGap(yeuCau, 3, yeuCau.AddDays(nopSauSoNgay), yeuCau.AddDays(5), soVongDaDocLai: 0);

        Assert.Equal(kyVong, kq.TinhTrang);
        Assert.Equal(new DateOnly(2026, 12, 4), kq.HanNop);
        Assert.Equal(kyVong == TinhTrangSuaGap.GVDDPhaiQuyetDinh ? "D08" : "D06", kq.MaNgoaiLe);
        Assert.Equal(kyVong != TinhTrangSuaGap.GVDDPhaiQuyetDinh, kq.Dat);
        Assert.Equal(MaLuat.SuaGap, kq.MaLuat);
        Assert.False(string.IsNullOrEmpty(kq.LyDo));
    }

    [Fact]
    public void T16_Da_doc_lai_1_vong_thi_khong_mo_vong_2_va_xet_bao_ve_tu_choi_sua_gap()
    {
        var yeuCau = new DateOnly(2026, 12, 1);
        var kq = XetBaoVe.KiemTraSuaGap(yeuCau, 3, yeuCau.AddDays(1), yeuCau.AddDays(1), soVongDaDocLai: 1);
        Assert.Equal(TinhTrangSuaGap.GVDDPhaiQuyetDinh, kq.TinhTrang);

        var hoSo = HoSo.BaoVe(gvdd: KetQuaDocDuyetLoai.SuaGap) with { DaHetVongSuaGap = true };
        var ex = Assert.Throws<DomainException>(() => Xet(hoSo));
        Assert.Equal("UPM_D08_KHONG_MO_VONG_2", ex.Code);
    }

    [Theory]
    [InlineData(2, TinhTrangSuaGap.ChoNop)]
    [InlineData(3, TinhTrangSuaGap.ChoNop)]
    [InlineData(4, TinhTrangSuaGap.GVDDPhaiQuyetDinh)]
    public void D08_Chua_nop_ban_sua_het_han_GVDD_phai_quyet(int homNaySauSoNgay, TinhTrangSuaGap kyVong)
    {
        var yeuCau = new DateOnly(2026, 12, 1);

        var kq = XetBaoVe.KiemTraSuaGap(yeuCau, 3, null, yeuCau.AddDays(homNaySauSoNgay), 0);

        Assert.Equal(kyVong, kq.TinhTrang);
        Assert.False(string.IsNullOrEmpty(kq.LyDo));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void D06_So_ngay_sua_gap_ngoai_1_den_3_bi_tu_choi(int soNgay)
    {
        var ex = Assert.Throws<DomainException>(() => XetBaoVe.KiemTraSuaGap(new DateOnly(2026, 12, 1), soNgay, null, new DateOnly(2026, 12, 1), 0));
        Assert.Equal("UPM_D06_SO_NGAY_SUA_GAP_KHONG_HOP_LE", ex.Code);
    }

    [Fact]
    public void T18_Nhom_3_SV_1_SV_no_TC_HOAN_2_SV_con_lai_van_DUOC_BAO_VE()
    {
        var sv1 = Xet(HoSo.BaoVe(DuTC(), gpa: 3.10m));
        var sv2 = Xet(HoSo.BaoVe(DuTC().No(3), gpa: 2.80m));
        var sv3 = Xet(HoSo.BaoVe(DuTC(), gpa: 2.20m));

        Assert.Equal(KetLuanXetBaoVe.DUOC_BAO_VE, sv1.KetLuan);
        Assert.Equal(KetLuanXetBaoVe.HOAN_BAO_VE, sv2.KetLuan);
        Assert.Equal(KetLuanXetBaoVe.DUOC_BAO_VE, sv3.KetLuan);

        // Điểm tính riêng từng SV dù cùng đề tài, cùng hội đồng.
        var thamSo = ThamSoTinhDiem.Tu(RuleSetMau.Entity2021(), null);
        var d1 = TinhDiemDATN.Tinh(new DiemThanhPhan(8.0m, 7.0m), HoiDongMau.Phieu(8.0m, 8.0m, 7.0m), thamSo);
        var d3 = TinhDiemDATN.Tinh(new DiemThanhPhan(6.0m, 6.0m), HoiDongMau.Phieu(5.0m, 5.0m, 5.0m), thamSo);
        Assert.NotEqual(d1.DiemDATN, d3.DiemDATN);
        Assert.True(d1.Dat);
        Assert.False(d3.Dat);
    }

    // ---------- Ngoại lệ mục 10 ----------

    [Fact]
    public void D01_No_TC_luc_xet_bao_ve_HOAN_den_het_hoc_ky_lien_ke()
    {
        var kq = Xet(HoSo.BaoVe(DuTC().No(3).DangHoc(2)));

        Assert.Equal(KetLuanXetBaoVe.HOAN_BAO_VE, kq.KetLuan);
        Assert.Equal("D01", kq.MaNgoaiLe);
        Assert.Equal(HocKy.Parse("2026.2"), kq.HocKyHanHoan);
        Assert.Equal(new DateOnly(2027, 6, 30), kq.NgayHanHoan);
        Assert.Equal(5, kq.TCConThieu.Sum(c => c.SoTC));
        Assert.Contains("PHAI_LAM_LAI", kq.LyDo, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(2027, 6, 30, true)]
    [InlineData(2027, 7, 1, false)]
    public void D01_Qua_han_hoan_chuyen_PHAI_LAM_LAI(int nam, int thang, int ngay, bool conHan)
    {
        var kq = XetBaoVe.KiemTraHanHoan(new DateOnly(2027, 6, 30), new DateOnly(nam, thang, ngay));

        Assert.Equal(conHan, kq.Dat);
        Assert.Equal("D01", kq.MaNgoaiLe);
    }

    [Fact]
    public void D02_GPA_duoi_1_90_HOAN_BAO_VE()
    {
        var kq = Xet(HoSo.BaoVe(gpa: 1.85m));

        Assert.Equal(KetLuanXetBaoVe.HOAN_BAO_VE, kq.KetLuan);
        Assert.Equal("D02", kq.MaNgoaiLe);
    }

    [Fact]
    public void D03_Chua_qua_TTTN_xu_ly_nhu_K4_HOAN_BAO_VE()
    {
        var kq = Xet(HoSo.BaoVe(HocTap.DungTienDo().NenDaDat().ChuaHoc(10, LoaiHocPhan.DATN).DangHoc(5, LoaiHocPhan.TTTN)));

        Assert.Equal(KetLuanXetBaoVe.HOAN_BAO_VE, kq.KetLuan);
        Assert.True(kq[MaLuat.K4].Dat);
        Assert.False(kq[MaLuat.K6].Dat);
        Assert.Equal("D03", kq.MaNgoaiLe);
    }

    [Fact]
    public void D04_GPA_dung_bien_1_895_dat_nho_lam_tron_decimal()
    {
        var kq = Xet(HoSo.BaoVe(gpa: 1.895m));

        Assert.True(kq[MaLuat.K5].Dat);
        Assert.Equal("D04", kq[MaLuat.K5].MaNgoaiLe);
    }

    [Fact]
    public void D05_GVHD_khong_dong_y_PHAI_LAM_LAI_va_bat_buoc_ly_do()
    {
        var kq = Xet(HoSo.BaoVe(gvhd: false));
        Assert.Equal(KetLuanXetBaoVe.PHAI_LAM_LAI, kq.KetLuan);
        Assert.Equal("D05", kq.MaNgoaiLe);
        Assert.Contains("Không đạt yêu cầu tiến độ.", kq.LyDo, StringComparison.Ordinal);

        var ex = Assert.Throws<DomainException>(() => Xet(HoSo.BaoVe(gvhd: false) with { LyDoGVHD = " " }));
        Assert.Equal("UPM_D05_THIEU_LY_DO_GVHD", ex.Code);
    }

    [Fact]
    public void D06_GVDD_loi_nho_YEU_CAU_CHINH_SUA()
    {
        var kq = Xet(HoSo.BaoVe(gvdd: KetQuaDocDuyetLoai.SuaGap));

        Assert.Equal(KetLuanXetBaoVe.YEU_CAU_CHINH_SUA, kq.KetLuan);
        Assert.Equal("D06", kq.MaNgoaiLe);
        Assert.Null(kq.NgayHanHoan);
    }

    [Fact]
    public void D07_GVDD_khong_dong_y_PHAI_LAM_LAI()
    {
        var kq = Xet(HoSo.BaoVe(gvdd: KetQuaDocDuyetLoai.KhongDongY));

        Assert.Equal(KetLuanXetBaoVe.PHAI_LAM_LAI, kq.KetLuan);
        Assert.Equal("D07", kq.MaNgoaiLe);
    }

    [Fact]
    public void C06_Ky_luat_phat_sinh_kiem_tra_lai_K1_tai_ngay_bao_ve()
    {
        var kq = Xet(HoSo.BaoVe(kyLuat: [HoSo.HinhSu(new DateOnly(2026, 9, 1))]));

        Assert.Equal(KetLuanXetBaoVe.PHAI_LAM_LAI, kq.KetLuan);
        Assert.Equal("C06", kq.MaNgoaiLe);
    }

    [Fact]
    public void K4_khong_tinh_HP_thay_the_DATN_va_HP_khong_tinh_diem()
    {
        var kq = Xet(HoSo.BaoVe(DuTC().ThayTheDATN(7).KhongTinhDiem(2)));

        Assert.True(kq[MaLuat.K4].Dat);
        Assert.Empty(kq.TCConThieu);
    }
}

internal static class HoiDongMau
{
    /// <summary>HĐ 3 người trực tiếp, đều có mặt, phiếu theo thứ tự CT, TK, UV.</summary>
    public static IReadOnlyList<ThanhVienChamDiem> Phieu(decimal ct, decimal tk, decimal uv) =>
    [
        new(1, ChucDanhHoiDong.ChuTich, true, ct),
        new(2, ChucDanhHoiDong.ThuKy, true, tk),
        new(3, ChucDanhHoiDong.UyVien, true, uv),
    ];
}
