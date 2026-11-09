using Upm.Domain.Enums;
using Upm.Domain.Rules;

namespace Upm.Domain.Tests.Rules;

/// <summary>Luật tiến độ 3 mốc C01–C07 (đặc tả mục 5). Tên test theo mã luật (CLAUDE.md §6).</summary>
public sealed class TienDoRulesTests
{
    private static readonly DateOnly BatDau = new(2026, 9, 7);
    private static readonly DateOnly NopQuyen = new(2026, 12, 4);

    [Theory]
    [InlineData(MaMocTienDo.M1, 3, 4)]
    [InlineData(MaMocTienDo.M2, 8, 9)]
    [InlineData(MaMocTienDo.M3, 12, 13)]
    public void Tuan_moc_theo_dac_ta(MaMocTienDo moc, byte tuTu, byte den)
    {
        Assert.Equal((tuTu, den), TienDoRules.TuanMoc(moc));
    }

    [Fact]
    public void Han_mac_dinh_cuoi_tuan_4_cuoi_tuan_9_va_ngay_nop_quyen()
    {
        Assert.Equal(new DateOnly(2026, 10, 4), TienDoRules.HanMacDinh(MaMocTienDo.M1, BatDau, NopQuyen));
        Assert.Equal(new DateOnly(2026, 11, 8), TienDoRules.HanMacDinh(MaMocTienDo.M2, BatDau, NopQuyen));
        Assert.Equal(NopQuyen, TienDoRules.HanMacDinh(MaMocTienDo.M3, BatDau, NopQuyen));
    }

    [Fact]
    public void C02_Mo_c2_duoi_yeu_cau_la_canh_bao_va_dat_khi_du()
    {
        Assert.Equal(KetQuaTienDo.CanhBao, TienDoRules.PhanLoai(55m, TienDoRules.YeuCauToiThieu(MaMocTienDo.M2, null)));
        Assert.Equal(KetQuaTienDo.Dat, TienDoRules.PhanLoai(60m, TienDoRules.YeuCauToiThieu(MaMocTienDo.M2, null)));
        Assert.Equal(KetQuaTienDo.Dat, TienDoRules.PhanLoai(10m, TienDoRules.YeuCauToiThieu(MaMocTienDo.M1, null)));
    }

    [Fact]
    public void C02_Yeu_cau_M2_lay_tu_cau_hinh_va_M3_la_100()
    {
        Assert.Equal(70m, TienDoRules.YeuCauToiThieu(MaMocTienDo.M2, 70m));
        Assert.Equal(100m, TienDoRules.YeuCauToiThieu(MaMocTienDo.M3, null));
        Assert.Equal(KetQuaTienDo.CanhBao, TienDoRules.PhanLoai(99m, 100m));
    }

    [Fact]
    public void C01_Qua_han_chua_nop_thanh_KhongNop_con_truoc_han_chua_nop()
    {
        var han = new DateOnly(2026, 10, 4);

        Assert.Equal(KetQuaTienDo.KhongNop, TienDoRules.TrangThaiMoc(null, han.AddDays(1), han));
        Assert.Null(TienDoRules.TrangThaiMoc(null, han, han)); // đúng hạn chưa quá
        Assert.Equal(KetQuaTienDo.Dat, TienDoRules.TrangThaiMoc(KetQuaTienDo.Dat, han.AddDays(5), han));
    }

    [Fact]
    public void Nhac_truoc_han_trong_3_ngay()
    {
        var han = new DateOnly(2026, 10, 4);

        Assert.True(TienDoRules.NenNhacTruoc(han.AddDays(-3), han));
        Assert.False(TienDoRules.NenNhacTruoc(han.AddDays(-4), han));
        Assert.False(TienDoRules.NenNhacTruoc(han.AddDays(1), han));
    }

    [Fact]
    public void C04_Bo_hai_moc_tro_len_chi_goi_y_RUT_khong_tu_dong()
    {
        Assert.False(TienDoRules.NenGoiYRut([KetQuaTienDo.KhongNop, KetQuaTienDo.Dat]));
        Assert.True(TienDoRules.NenGoiYRut([KetQuaTienDo.KhongNop, KetQuaTienDo.KhongNop, null]));
    }

    [Fact]
    public void C07_Thieu_cam_ket_AI_bi_chan()
    {
        var kq = TienDoRules.KiemTraCamKetAI(false);

        Assert.False(kq.Dat);
        Assert.Equal("C07", kq.MaNgoaiLe);
        Assert.True(TienDoRules.KiemTraCamKetAI(true).Dat);
    }

    [Fact]
    public void C05_Gia_han_phai_sau_han_cu()
    {
        Assert.True(TienDoRules.KiemTraHanGiaHan(new DateOnly(2026, 10, 4), new DateOnly(2026, 10, 18)).Dat);
        Assert.False(TienDoRules.KiemTraHanGiaHan(new DateOnly(2026, 10, 4), new DateOnly(2026, 10, 4)).Dat);
    }
}
