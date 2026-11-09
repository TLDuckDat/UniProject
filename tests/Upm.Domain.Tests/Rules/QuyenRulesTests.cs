using Upm.Domain.Enums;
using Upm.Domain.Rules;

namespace Upm.Domain.Tests.Rules;

/// <summary>Luật nộp quyển, ý kiến, đọc duyệt, đạo văn D05–D13 (đặc tả mục 7). Tên test theo mã luật.</summary>
public sealed class QuyenRulesTests
{
    private static readonly DateOnly HomNay = new(2026, 11, 9);

    [Fact]
    public void D05_GVHD_khong_dong_y_bat_buoc_ly_do()
    {
        Assert.False(QuyenRules.KiemTraLyDoKhongDongY(false, null).Dat);
        Assert.False(QuyenRules.KiemTraLyDoKhongDongY(false, "  ").Dat);
        Assert.True(QuyenRules.KiemTraLyDoKhongDongY(false, "Chưa đạt yêu cầu nội dung.").Dat);
        Assert.True(QuyenRules.KiemTraLyDoKhongDongY(true, null).Dat);
    }

    [Fact]
    public void D06_Han_sua_gap_tu_1_den_so_ngay_cau_hinh()
    {
        Assert.True(QuyenRules.KiemTraSuaGap(1, HomNay, HomNay.AddDays(1), 3).Dat);
        Assert.True(QuyenRules.KiemTraSuaGap(1, HomNay, HomNay.AddDays(3), 3).Dat);
        Assert.False(QuyenRules.KiemTraSuaGap(1, HomNay, HomNay, 3).Dat);
        Assert.False(QuyenRules.KiemTraSuaGap(1, HomNay, HomNay.AddDays(4), 3).Dat);
    }

    [Fact]
    public void D08_Khong_co_vong_sua_gap_thu_hai()
    {
        var kq = QuyenRules.KiemTraSuaGap(2, HomNay, HomNay.AddDays(1), 3);

        Assert.False(kq.Dat);
        Assert.Equal("D08", kq.MaNgoaiLe);
    }

    [Fact]
    public void D09_GVDD_qua_han_doc_duyet_phai_bao_Truong_BM()
    {
        var nop = new DateTime(2026, 11, 1, 8, 0, 0);

        Assert.True(QuyenRules.QuaHanDocDuyet(nop, nop.AddDays(8), 7));
        Assert.False(QuyenRules.QuaHanDocDuyet(nop, nop.AddDays(7), 7));
    }

    [Fact]
    public void D10_Dong_y_bat_buoc_co_diem_rieng()
    {
        Assert.False(QuyenRules.KiemTraDiemGVDD(KetQuaDocDuyetLoai.DongY, null).Dat);
        Assert.True(QuyenRules.KiemTraDiemGVDD(KetQuaDocDuyetLoai.DongY, 8.5m).Dat);
        Assert.True(QuyenRules.KiemTraDiemGVDD(KetQuaDocDuyetLoai.SuaGap, null).Dat);
        Assert.True(QuyenRules.KiemTraDiemGVDD(KetQuaDocDuyetLoai.KhongDongY, null).Dat);
    }

    [Fact]
    public void D11_Trung_tren_nguong_bi_chan_bang_nguong_van_qua()
    {
        var vuot = QuyenRules.KiemTraDaoVan(25m, 20m);

        Assert.False(vuot.Dat);
        Assert.Equal("D11", vuot.MaNgoaiLe);
        Assert.True(QuyenRules.KiemTraDaoVan(20m, 20m).Dat);
    }

    [Fact]
    public void D12_Nop_quyen_qua_han_bi_chan_tru_khi_co_gia_han()
    {
        var han = new DateOnly(2026, 11, 1);

        Assert.False(QuyenRules.KiemTraHanNopQuyen(HomNay, han, null).Dat);
        Assert.True(QuyenRules.KiemTraHanNopQuyen(HomNay, han, HomNay).Dat);
        Assert.True(QuyenRules.KiemTraHanNopQuyen(han, han, null).Dat);
    }

    [Fact]
    public void D13_Ban_bao_ve_phai_trung_hash_ban_GVDD_da_doc()
    {
        const string h1 = "3f5a0c1d2e4b5f6a7c8d9e0f1a2b3c4d5e6f708192a3b4c5d6e7f8091a2b3c4d";
        const string h2 = "9a8b7c6d5e4f30201f1e2d3c4b5a69788796a5b4c3d2e1f00112233445566778";

        Assert.True(QuyenRules.KiemTraBanBaoVe(h1, h1.ToUpperInvariant()).Dat);
        var lech = QuyenRules.KiemTraBanBaoVe(h1, h2);
        Assert.False(lech.Dat);
        Assert.Equal("D13", lech.MaNgoaiLe);
    }
}
