using Upm.Domain.Common;
using Upm.Domain.Enums;
using Upm.Domain.Rules;

namespace Upm.Domain.Tests.Rules;

/// <summary>T10, T17, A15, A16, hạ hạng tốt nghiệp theo RuleSet, học kỳ, tham số quy chế.</summary>
public class XetTotNghiepVaRuleSetTests
{
    private static readonly AnhXaRuleSetKhoa[] BangKhoa =
    [
        new("UTC-2021", 1, 65),
        new("UTC-2021", 66, 66, CanXacNhan: true),
        new("UTC-2026", 67, null),
    ];

    // ---------- T10 ----------

    [Fact]
    public void T10_GPA_1_90_bao_ve_dat_1_95_xet_tot_nghiep_khong_dat()
    {
        var baoVe = XetBaoVe.Xet(HoSo.BaoVe(gpa: 1.90m), RuleSetMau.Utc2021, HocTap.KyXet, HoSo.NgayXet, LichHocKyGia.Instance);
        var totNghiep = XetTotNghiep.KiemTraGpa(1.95m, RuleSetMau.Utc2021);

        Assert.True(baoVe[MaLuat.K5].Dat);
        Assert.False(totNghiep.Dat);
        Assert.Equal(MaLuat.GpaTotNghiep, totNghiep.MaLuat);
        Assert.True(XetTotNghiep.KiemTraGpa(1.995m, RuleSetMau.Utc2021).Dat); // 1,995 → 2,00
    }

    // ---------- T17, A15, A16 ----------

    [Theory]
    [InlineData(64, "UTC-2021", false, "A15")]
    [InlineData(65, "UTC-2021", false, "A15")]
    [InlineData(66, "UTC-2021", true, "A16")]
    [InlineData(67, "UTC-2026", false, "A15")]
    [InlineData(70, "UTC-2026", false, "A15")]
    public void T17_RuleSet_theo_khoa_SV_khong_theo_dot(int khoa, string ruleSet, bool canXacNhan, string ma)
    {
        var kq = ChonRuleSet.TheoKhoa(khoa, BangKhoa);

        Assert.Equal(ruleSet, kq.MaRuleSet);
        Assert.Equal(canXacNhan, kq.CanXacNhan);
        Assert.Equal(ma, kq.MaNgoaiLe);
        Assert.True(kq.Dat);
        Assert.Equal(MaLuat.RuleSetTheoKhoa, kq.MaLuat);
        Assert.Contains($"K{khoa}", kq.LyDo, StringComparison.Ordinal);
    }

    [Fact]
    public void T17_K64_lam_lai_va_K67_cung_dot_ap_RuleSet_rieng_va_luu_RuleVersion()
    {
        var thamSo = new Dictionary<string, ThamSoQuyChe> { ["UTC-2021"] = RuleSetMau.Utc2021, ["UTC-2026"] = RuleSetMau.Utc2026 };
        var hocTap = HocTap.DungTienDo().NenDaDat().Dat(5, LoaiHocPhan.TTTN);

        var k64 = XetGiao.Xet(HoSo.Giao(hocTap), thamSo[ChonRuleSet.TheoKhoa(64, BangKhoa).MaRuleSet], HocTap.KyXet, HoSo.NgayXet);
        var k67 = XetGiao.Xet(HoSo.Giao(hocTap), thamSo[ChonRuleSet.TheoKhoa(67, BangKhoa).MaRuleSet], HocTap.KyXet, HoSo.NgayXet);

        Assert.Equal("UTC-2021", k64.RuleVersion);
        Assert.Equal("UTC-2026", k67.RuleVersion);
    }

    [Fact]
    public void Khoa_khong_co_hoac_trung_RuleSet_bi_tu_choi()
    {
        Assert.Equal("UPM_RULESET_KHONG_XAC_DINH",
            Assert.Throws<DomainException>(() => ChonRuleSet.TheoKhoa(67, [new AnhXaRuleSetKhoa("UTC-2021", 1, 65)])).Code);
        Assert.Equal("UPM_RULESET_KHONG_XAC_DINH",
            Assert.Throws<DomainException>(() => ChonRuleSet.TheoKhoa(67, [.. BangKhoa, new AnhXaRuleSetKhoa("UTC-X", 60, null)])).Code);
    }

    // ---------- Hạ hạng ----------

    public static TheoryData<string, int, MucKyLuat?, bool, XepLoaiTotNghiep, XepLoaiTotNghiep> HaHang => new()
    {
        // RuleSet,   TC học lại /150, kỷ luật,             liêm chính, xếp loại,                 sau xét
        { "UTC-2021", 7, null, false, XepLoaiTotNghiep.Gioi, XepLoaiTotNghiep.Gioi },       // 4,67% ≤ 5%
        { "UTC-2021", 8, null, false, XepLoaiTotNghiep.Gioi, XepLoaiTotNghiep.Kha },        // 5,33% > 5%
        { "UTC-2026", 8, null, false, XepLoaiTotNghiep.Gioi, XepLoaiTotNghiep.Gioi },       // 5,33% ≤ 10%
        { "UTC-2026", 15, null, false, XepLoaiTotNghiep.XuatSac, XepLoaiTotNghiep.XuatSac }, // đúng 10% – chưa vượt
        { "UTC-2026", 16, null, false, XepLoaiTotNghiep.XuatSac, XepLoaiTotNghiep.Gioi },   // 10,67% > 10%
        { "UTC-2021", 0, MucKyLuat.CanhCao, false, XepLoaiTotNghiep.XuatSac, XepLoaiTotNghiep.Gioi },
        { "UTC-2021", 0, MucKyLuat.KhienTrach, false, XepLoaiTotNghiep.XuatSac, XepLoaiTotNghiep.XuatSac },
        { "UTC-2021", 0, null, true, XepLoaiTotNghiep.Gioi, XepLoaiTotNghiep.Gioi },        // 2021 không xét liêm chính
        { "UTC-2026", 0, null, true, XepLoaiTotNghiep.Gioi, XepLoaiTotNghiep.Kha },
        { "UTC-2021", 20, MucKyLuat.CanhCao, false, XepLoaiTotNghiep.Kha, XepLoaiTotNghiep.Kha }, // chỉ hạ Giỏi/Xuất sắc
    };

    [Theory]
    [MemberData(nameof(HaHang))]
    public void Ha_hang_tot_nghiep_theo_RuleSet(string ruleSet, int tcHocLai, MucKyLuat? kyLuat, bool liemChinh,
        XepLoaiTotNghiep xepLoai, XepLoaiTotNghiep kyVong)
    {
        var rs = ruleSet == "UTC-2021" ? RuleSetMau.Utc2021 : RuleSetMau.Utc2026;
        IReadOnlyList<KyLuat> kl = kyLuat is { } m ? [new KyLuat(m, new DateOnly(2024, 1, 1), new DateOnly(2024, 6, 1), false, false)] : [];

        var kq = XetTotNghiep.HaHang(new HoSoXetHaHang(xepLoai, tcHocLai, 150, kl, liemChinh), rs);

        Assert.Equal(kyVong, kq.XepLoaiSauXet);
        Assert.Equal(kyVong == xepLoai, kq.Dat);
        Assert.Equal(ruleSet, kq.RuleVersion);
        Assert.Equal(MaLuat.HaHang, kq.MaLuat);
        Assert.Null(kq.MaNgoaiLe);
        Assert.Contains(kq.Dat ? "Giữ" : "Hạ", kq.LyDo, StringComparison.Ordinal);
    }

    [Fact]
    public void Cung_ho_so_hoc_lai_8_tren_150_TC_UTC_2021_ha_hang_UTC_2026_khong()
    {
        var hoSo = new HoSoXetHaHang(XepLoaiTotNghiep.Gioi, 8, 150, []);

        Assert.True(XetTotNghiep.HaHang(hoSo, RuleSetMau.Utc2021).BiHaHang);
        Assert.False(XetTotNghiep.HaHang(hoSo, RuleSetMau.Utc2026).BiHaHang);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 150)]
    public void Ha_hang_du_lieu_khong_hop_le_bi_tu_choi(int tcHocLai, int tong)
    {
        var ex = Assert.Throws<DomainException>(() =>
            XetTotNghiep.HaHang(new HoSoXetHaHang(XepLoaiTotNghiep.Gioi, tcHocLai, tong, []), RuleSetMau.Utc2021));
        Assert.Equal("UPM_HA_HANG_DU_LIEU_KHONG_HOP_LE", ex.Code);
    }

    // ---------- Tham số & học kỳ ----------

    [Fact]
    public void ThamSoQuyChe_doc_tu_RuleSet()
    {
        var ts = ThamSoQuyChe.Tu(RuleSetMau.Entity2026(), haHangKhiViPhamLiemChinh: true);

        Assert.Equal("UTC-2026", ts.MaRuleSet);
        Assert.Equal(10, ts.MaxTCChuaTichLuy);
        Assert.Equal(1.90m, ts.GpaBaoVeMin);
        Assert.Equal(2.00m, ts.GpaTotNghiepMin);
        Assert.Equal(5.5m, ts.DiemDatDATN);
        Assert.Equal(3, ts.MinTVHoiDong);
        Assert.Equal(5, ts.MaxTVHoiDong);
        Assert.Equal(10.0m, ts.NguongHaHangHocLaiPct);
        Assert.True(ts.HaHangKhiKyLuat);
        Assert.True(ts.HaHangKhiViPhamLiemChinh);
    }

    [Fact]
    public void Nguong_tin_chi_lay_tu_RuleSet_khong_hard_code()
    {
        var rs = RuleSetMau.Utc2021 with { MaxTCChuaTichLuy = 12 };
        var hocTap = HocTap.DungTienDo().NenDaDat().Dat(5, LoaiHocPhan.TTTN).No(11);

        Assert.Equal(KetLuanXetGiao.DU_DK, XetGiao.Xet(HoSo.Giao(hocTap), rs, HocTap.KyXet, HoSo.NgayXet).KetLuan);
    }

    [Theory]
    [InlineData("2024.1", "2024.2")]
    [InlineData("2024.2", "2025.1")]
    [InlineData("2024.3", "2025.1")]
    public void HocKy_lien_ke(string ky, string keTiep)
    {
        Assert.Equal(keTiep, HocKy.Parse(ky).KeTiep().ToString());
    }

    [Theory]
    [InlineData("2024")]
    [InlineData("2024.4")]
    [InlineData("abcd.1")]
    [InlineData("2024.-1")]
    [InlineData("1800.1")]
    [InlineData("")]
    public void HocKy_sai_dinh_dang_bi_tu_choi(string giaTri)
    {
        Assert.Equal("UPM_HOC_KY_KHONG_HOP_LE", Assert.Throws<DomainException>(() => HocKy.Parse(giaTri)).Code);
    }

    [Fact]
    public void HocKy_so_sanh()
    {
        var a = HocKy.Parse("2024.2");
        var b = HocKy.Parse("2025.1");

        Assert.True(a < b);
        Assert.True(b > a);
        Assert.True(a <= HocKy.Parse("2024.2"));
        Assert.True(b >= a);
        Assert.False(a > b);
        Assert.True(HocKy.Parse("2024.1") < a);
        Assert.Equal(2024, a.NamHoc);
        Assert.Equal(2, a.Ky);
    }
}
