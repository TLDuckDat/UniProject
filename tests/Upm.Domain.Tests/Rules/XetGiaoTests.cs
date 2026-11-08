using Upm.Domain.Enums;
using Upm.Domain.Rules;

namespace Upm.Domain.Tests.Rules;

/// <summary>Xét giao G1–G3 (đặc tả mục 6), test biên T01–T07, ngoại lệ A01–A14.</summary>
public class XetGiaoTests
{
    private static KetQuaXetGiao Xet(HoSoXetGiao hoSo, ThamSoQuyChe? rs = null) =>
        XetGiao.Xet(hoSo, rs ?? RuleSetMau.Utc2021, HocTap.KyXet, HoSo.NgayXet);

    private static IEnumerable<ChiTietHocPhanChuaTichLuy> TinhVao(KetQuaTCChuaTichLuy kq) => kq.ChiTiet.Where(c => !c.BiLoaiTru);

    private static HocTap SvDaQuaTTTN() => HocTap.DungTienDo().NenDaDat().Dat(5, LoaiHocPhan.TTTN).ChuaHoc(10, LoaiHocPhan.DATN);

    // ---------- Test biên mục 11 ----------

    [Fact]
    public void T01_No_6_TC_dang_hoc_4_TC_bang_10_duoc_giao_co_NO_TIN_CHI()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN().No(3).No(3).DangHoc(4)));

        Assert.Equal(KetLuanXetGiao.DU_DK, kq.KetLuan);
        Assert.Equal(10, kq.TinChi.TongTC);
        Assert.Equal([CoXetGiao.NO_TIN_CHI], kq.Co);
        Assert.True(kq.Dat);
    }

    [Fact]
    public void T02_No_6_TC_dang_hoc_5_TC_bang_11_khong_du_DK()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN().No(3).No(3).DangHoc(5)));

        Assert.Equal(KetLuanXetGiao.KHONG_DU_DK, kq.KetLuan);
        Assert.Equal(11, kq.TinChi.TongTC);
        Assert.Equal("A02", kq.CacLuat.Single(l => l.MaLuat == MaLuat.G3).MaNgoaiLe);
    }

    [Fact]
    public void T03_Dung_tien_do_no_8_TC_chua_hoc_TTTN_duoc_giao_co_CHUA_QUA_TTTN()
    {
        var kq = Xet(HoSo.Giao(HocTap.DungTienDo().NenDaDat().No(4).No(4).ChuaHoc(5, LoaiHocPhan.TTTN)));

        Assert.Equal(KetLuanXetGiao.DU_DK, kq.KetLuan);
        Assert.Equal(8, kq.TinChi.TongTC);
        Assert.Contains(CoXetGiao.CHUA_QUA_TTTN, kq.Co);
        Assert.Contains(CoXetGiao.NO_TIN_CHI, kq.Co);
        var tttn = kq.TinChi.ChiTiet.Single(c => c.LoaiHP == LoaiHocPhan.TTTN);
        Assert.True(tttn.BiLoaiTru);
        Assert.Equal("A06", tttn.MaNgoaiLe);
    }

    [Fact]
    public void T04_Tre_tien_do_no_8_TC_chua_co_diem_TTTN_13_TC_khong_du_DK()
    {
        var kq = Xet(HoSo.Giao(HocTap.TreTienDo().NenDaDat().No(4).No(4).ChuaHoc(5, LoaiHocPhan.TTTN)));

        Assert.Equal(KetLuanXetGiao.KHONG_DU_DK, kq.KetLuan);
        Assert.Equal(13, kq.TinChi.TongTC);
        Assert.False(kq.TinChi.DungTienDo);
    }

    [Fact]
    public void T05_Tre_tien_do_no_8_TC_da_dat_TTTN_duoc_giao()
    {
        var kq = Xet(HoSo.Giao(HocTap.TreTienDo().NenDaDat().Dat(5, LoaiHocPhan.TTTN).No(4).No(4)));

        Assert.Equal(KetLuanXetGiao.DU_DK, kq.KetLuan);
        Assert.Equal(8, kq.TinChi.TongTC);
        Assert.DoesNotContain(CoXetGiao.CHUA_QUA_TTTN, kq.Co);
    }

    [Fact]
    public void T06_No_GDQP_8_TC_va_GDTC_3_TC_duoc_giao_va_K4_khong_anh_huong()
    {
        var hocTap = SvDaQuaTTTN().No(8, LoaiHocPhan.GDQP).No(3, LoaiHocPhan.GDTC);

        var giao = Xet(HoSo.Giao(hocTap));
        var baoVe = XetBaoVe.Xet(HoSo.BaoVe(hocTap), RuleSetMau.Utc2021, HocTap.KyXet, HoSo.NgayXet, LichHocKyGia.Instance);

        Assert.Equal(KetLuanXetGiao.DU_DK, giao.KetLuan);
        Assert.Equal(0, giao.TinChi.TongTC);
        Assert.Empty(giao.Co);
        Assert.True(baoVe[MaLuat.K4].Dat);
    }

    [Fact]
    public void T07_Diem_I_3_TC_cong_no_8_TC_bang_11_khong_du_DK()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN().DiemI(3).No(4).No(4)));

        Assert.Equal(KetLuanXetGiao.KHONG_DU_DK, kq.KetLuan);
        Assert.Equal(11, kq.TinChi.TongTC);
    }

    // ---------- Ngoại lệ mục 10 ----------

    [Fact]
    public void A01_No_trong_nguong_duoc_giao_co_NO_TIN_CHI()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN().No(4)));

        Assert.Equal(KetLuanXetGiao.DU_DK, kq.KetLuan);
        Assert.Equal([CoXetGiao.NO_TIN_CHI], kq.Co);
        Assert.Equal("A01", kq.MaNgoaiLe);
    }

    [Fact]
    public void A02_No_qua_10_TC_khong_du_DK_va_liet_ke_hoc_phan_no()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN().No(6).No(5)));

        Assert.Equal(KetLuanXetGiao.KHONG_DU_DK, kq.KetLuan);
        Assert.Equal(2, kq.TinChi.ChiTiet.Count(c => !c.BiLoaiTru && c.LoaiNo == LoaiNo.No));
        Assert.Contains("11 TC > 10", kq.LyDo, StringComparison.Ordinal);
        Assert.Equal("A02", kq.TinChi.MaNgoaiLe);
    }

    [Theory]
    [InlineData(9, KetLuanXetGiao.DU_DK)]
    [InlineData(10, KetLuanXetGiao.DU_DK)]
    [InlineData(11, KetLuanXetGiao.KHONG_DU_DK)]
    public void A03_No_dung_10_TC_van_duoc_giao(int tcNo, KetLuanXetGiao kyVong)
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN().No(tcNo)));

        Assert.Equal(kyVong, kq.KetLuan);
        if (tcNo == 10)
        {
            Assert.Equal("A03", kq.MaNgoaiLe);
        }
    }

    [Fact]
    public void A04_Mon_dang_hoc_chua_co_diem_tinh_la_chua_tich_luy()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN().DangHoc(4)));

        var ct = Assert.Single(kq.TinChi.ChiTiet, c => !c.BiLoaiTru);
        Assert.Equal(LoaiNo.DangHoc, ct.LoaiNo);
        Assert.Equal("A04", ct.MaNgoaiLe);
        Assert.Equal(4, kq.TinChi.TongTC);
    }

    [Theory]
    [InlineData(LoaiHocPhan.GDQP)]
    [InlineData(LoaiHocPhan.GDTC)]
    [InlineData(LoaiHocPhan.DATN)]
    public void A05_No_GDQP_GDTC_DATN_khong_cong_vao_gioi_han(LoaiHocPhan loai)
    {
        var kq = Xet(HoSo.Giao(HocTap.DungTienDo().NenDaDat().Dat(5, LoaiHocPhan.TTTN).No(12, loai)));

        Assert.Equal(KetLuanXetGiao.DU_DK, kq.KetLuan);
        Assert.Equal(0, kq.TinChi.TongTC);
        var ct = Assert.Single(kq.TinChi.ChiTiet);
        Assert.True(ct.BiLoaiTru);
        Assert.Equal("A05", ct.MaNgoaiLe);
    }

    [Fact]
    public void A06_Dung_tien_do_chua_hoc_TTTN_khong_cong_va_gan_co_CHUA_QUA_TTTN()
    {
        var kq = Xet(HoSo.Giao(HocTap.DungTienDo().NenDaDat().ChuaHoc(5, LoaiHocPhan.TTTN)));

        Assert.Equal(KetLuanXetGiao.DU_DK, kq.KetLuan);
        Assert.Equal(0, kq.TinChi.TongTC);
        Assert.Equal([CoXetGiao.CHUA_QUA_TTTN], kq.Co);
        Assert.Equal("A06", kq.MaNgoaiLe);
    }

    [Fact]
    public void A06_Dung_tien_do_dang_hoc_TTTN_dau_ky_cung_khong_cong()
    {
        var kq = Xet(HoSo.Giao(HocTap.DungTienDo().NenDaDat().DangHoc(5, LoaiHocPhan.TTTN)));

        Assert.Equal(0, kq.TinChi.TongTC);
        Assert.Equal(LoaiNo.DangHoc, kq.TinChi.ChiTiet.Single().LoaiNo);
    }

    [Fact]
    public void A07_Tre_tien_do_chua_co_diem_TTTN_bi_cong_TC_TTTN()
    {
        var kq = Xet(HoSo.Giao(HocTap.TreTienDo().NenDaDat().No(2).No(5, LoaiHocPhan.TTTN)));

        Assert.Equal(KetLuanXetGiao.DU_DK, kq.KetLuan);
        Assert.Equal(7, kq.TinChi.TongTC);
        var tttn = kq.TinChi.ChiTiet.Single(c => c.LoaiHP == LoaiHocPhan.TTTN);
        Assert.Equal(LoaiNo.TTTNCong, tttn.LoaiNo);
        Assert.False(tttn.BiLoaiTru);
        Assert.Equal("A07", tttn.MaNgoaiLe);
        Assert.Equal([CoXetGiao.NO_TIN_CHI, CoXetGiao.CHUA_QUA_TTTN], kq.Co);
    }

    [Fact]
    public void A08_Diem_I_va_X_chua_tich_luy()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN().DiemI(3).DiemX(2)));

        Assert.Equal(5, kq.TinChi.TongTC);
        Assert.Equal([LoaiNo.DiemI, LoaiNo.DiemX], TinhVao(kq.TinChi).Select(c => c.LoaiNo));
        Assert.All(TinhVao(kq.TinChi), c => Assert.Equal("A08", c.MaNgoaiLe));
    }

    [Fact]
    public void A09_Hoc_phan_R_tinh_la_da_tich_luy()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN().DiemR(12)));

        Assert.Equal(KetLuanXetGiao.DU_DK, kq.KetLuan);
        Assert.Equal(0, kq.TinChi.TongTC);
        Assert.Empty(TinhVao(kq.TinChi));
    }

    [Fact]
    public void A10_Dang_dinh_chi_hoc_tap_khong_du_DK_du_tin_chi_du()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN(), kyLuat: [HoSo.DinhChi(new DateOnly(2026, 8, 1), new DateOnly(2026, 12, 31))]));

        Assert.Equal(KetLuanXetGiao.KHONG_DU_DK, kq.KetLuan);
        Assert.Equal("A10", kq.MaNgoaiLe);
        Assert.False(kq.CacLuat.Single(l => l.MaLuat == MaLuat.G1).Dat);
    }

    [Fact]
    public void A10_Truy_cuu_hinh_su_khong_du_DK()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN(), kyLuat: [HoSo.HinhSu(new DateOnly(2026, 1, 1))]));

        Assert.Equal(KetLuanXetGiao.KHONG_DU_DK, kq.KetLuan);
        Assert.Contains("hình sự", kq.LyDo, StringComparison.Ordinal);
    }

    [Fact]
    public void A10_Dinh_chi_da_het_han_thi_khong_chan()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN(), kyLuat: [HoSo.DinhChi(new DateOnly(2025, 1, 1), new DateOnly(2025, 6, 30))]));

        Assert.Equal(KetLuanXetGiao.DU_DK, kq.KetLuan);
    }

    [Fact]
    public void A11_Dang_nghi_hoc_tam_thoi_khong_du_DK()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN(), trangThai: TrangThaiHocTap.NghiTamThoi));

        Assert.Equal(KetLuanXetGiao.KHONG_DU_DK, kq.KetLuan);
        Assert.Equal("A11", kq.MaNgoaiLe);
    }

    [Theory]
    [InlineData(TrangThaiHocTap.BuocThoiHoc)]
    [InlineData(TrangThaiHocTap.DaThoiHoc)]
    [InlineData(TrangThaiHocTap.DaTotNghiep)]
    public void A12_Trang_thai_hoc_tap_khong_hop_le_khong_du_DK(TrangThaiHocTap trangThai)
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN(), trangThai: trangThai));

        Assert.Equal(KetLuanXetGiao.KHONG_DU_DK, kq.KetLuan);
        Assert.Equal("A12", kq.MaNgoaiLe);
    }

    [Fact]
    public void A12_Vuot_thoi_gian_hoc_toi_da_khong_du_DK()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN(), hanToiDa: new DateOnly(2026, 8, 31)));

        Assert.Equal(KetLuanXetGiao.KHONG_DU_DK, kq.KetLuan);
        Assert.Equal("A12", kq.MaNgoaiLe);
    }

    [Fact]
    public void A12_Tinh_han_toi_da_bang_2_lan_thoi_gian_chuan()
    {
        Assert.Equal(new DateOnly(2032, 9, 5), XetGiao.TinhHanToiDa(new DateOnly(2022, 9, 5), 5m, 2.0m));
        Assert.Equal(new DateOnly(2031, 4, 5), XetGiao.TinhHanToiDa(new DateOnly(2022, 9, 5), 4.5m, 1.9m));
    }

    [Fact]
    public void A13_Khoi_Ky_thuat_khong_co_HP_thay_the_khong_du_DK()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN().No(12), coHPThayThe: false));

        Assert.Equal(KetLuanXetGiao.KHONG_DU_DK, kq.KetLuan);
        Assert.Equal("A13", kq.MaNgoaiLe);
        Assert.False(kq.Dat);
    }

    [Fact]
    public void A14_Khoi_Kinh_te_co_HP_thay_the_chuyen_thay_the()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN().ThayTheDATN(7).No(12), coHPThayThe: true));

        Assert.Equal(KetLuanXetGiao.KHONG_DU_DK_CHUYEN_THAY_THE, kq.KetLuan);
        Assert.Equal("A14", kq.MaNgoaiLe);
        Assert.Equal(12, kq.TinChi.TongTC); // HP thay thế ĐATN không tính là nợ
        Assert.Contains("thay thế", kq.LyDo, StringComparison.Ordinal);
    }

    [Fact]
    public void A14_Ky_luat_uu_tien_hon_chuyen_thay_the()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN().No(12), coHPThayThe: true, kyLuat: [HoSo.HinhSu(new DateOnly(2026, 1, 1))]));

        Assert.Equal(KetLuanXetGiao.KHONG_DU_DK, kq.KetLuan);
        Assert.Equal("A10", kq.MaNgoaiLe);
    }

    // ---------- Chi tiết tính TC ----------

    [Fact]
    public void Hoc_phan_khong_tinh_diem_khong_xet()
    {
        var kq = TinhTCChuaTichLuy.Tinh(SvDaQuaTTTN().KhongTinhDiem(4).Build(), RuleSetMau.Utc2021, HocTap.KyXet);

        Assert.Empty(TinhVao(kq));
        Assert.Null(kq.MaNgoaiLe);
        Assert.Equal("UTC-2021", kq.RuleVersion);
        Assert.Contains("= 0", kq.LyDo, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(TrangThaiDiem.Dat, 0)]
    [InlineData(TrangThaiDiem.R, 0)]
    [InlineData(TrangThaiDiem.Rot, 3)]
    [InlineData(TrangThaiDiem.I, 3)]
    public void Hoc_lai_lay_ket_qua_lan_dat_bat_ky(TrangThaiDiem lan2, int tcKyVong)
    {
        var kq = TinhTCChuaTichLuy.Tinh(SvDaQuaTTTN().HocLai(3, lan2).Build(), RuleSetMau.Utc2021, HocTap.KyXet);

        Assert.Equal(tcKyVong, kq.TongTC);
        if (lan2 == TrangThaiDiem.I)
        {
            Assert.Equal(LoaiNo.DiemI, TinhVao(kq).Single().LoaiNo); // lần học mới nhất quyết định loại nợ
        }
    }

    [Fact]
    public void Du_DK_khong_no_khong_co()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN()));

        Assert.Equal(KetLuanXetGiao.DU_DK, kq.KetLuan);
        Assert.Null(kq.MaNgoaiLe);
        Assert.Equal("Đủ điều kiện giao ĐATN.", kq.LyDo);
        Assert.Equal(MaLuat.XetGiao, kq.MaLuat);
        Assert.Equal("UTC-2021", kq.RuleVersion);
        Assert.Equal(HocTap.KyXet, kq.KyXet);
        Assert.Equal(HoSo.NgayXet, kq.NgayXet);
        Assert.Equal([MaLuat.G1, MaLuat.G2, MaLuat.G3], kq.CacLuat.Select(l => l.MaLuat));
    }

    [Fact]
    public void Khong_du_DK_luu_du_ca_3_luat_de_giai_trinh()
    {
        var kq = Xet(HoSo.Giao(SvDaQuaTTTN().No(11), trangThai: TrangThaiHocTap.NghiTamThoi));

        Assert.Equal(2, kq.CacLuat.Count(l => !l.Dat));
        Assert.Contains("nghỉ học tạm thời", kq.LyDo, StringComparison.Ordinal);
        Assert.Contains("Nợ 11 TC", kq.LyDo, StringComparison.Ordinal);
    }
}
