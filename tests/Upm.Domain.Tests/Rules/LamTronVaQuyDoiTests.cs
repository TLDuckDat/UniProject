using Upm.Domain.Common;
using Upm.Domain.Rules;

namespace Upm.Domain.Tests.Rules;

public class LamTronVaQuyDoiTests
{
    [Theory]
    [InlineData("1.895", "1.90")] // D04, T08
    [InlineData("1.8949", "1.89")] // T09
    [InlineData("1.905", "1.91")]
    [InlineData("2.004", "2.00")]
    [InlineData("3.995", "4.00")]
    public void LamTron_Gpa_2_chu_so_half_up(string tho, string kyVong)
    {
        Assert.Equal(decimal.Parse(kyVong, System.Globalization.CultureInfo.InvariantCulture),
            LamTron.Gpa(decimal.Parse(tho, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData(5.55, 5.6)]
    [InlineData(7.25, 7.3)]
    [InlineData(5.45, 5.5)]
    [InlineData(5.44, 5.4)]
    [InlineData(6.666666, 6.7)]
    public void LamTron_Diem_1_chu_so_half_up(decimal tho, decimal kyVong)
    {
        Assert.Equal(kyVong, LamTron.Diem(tho));
    }

    /// <summary>Bảng quy đổi mục 8 – kiểm cả hai biên của mỗi mức.</summary>
    [Theory]
    [InlineData(10.0, "A+", 4.0)]
    [InlineData(9.5, "A+", 4.0)]
    [InlineData(9.4, "A", 3.8)]
    [InlineData(8.5, "A", 3.8)]
    [InlineData(8.4, "B+", 3.5)]
    [InlineData(8.0, "B+", 3.5)]
    [InlineData(7.9, "B", 3.0)]
    [InlineData(7.0, "B", 3.0)]
    [InlineData(6.9, "C+", 2.5)]
    [InlineData(6.0, "C+", 2.5)]
    [InlineData(5.9, "C", 2.0)]
    [InlineData(5.5, "C", 2.0)]
    [InlineData(5.4, "D+", 1.5)]
    [InlineData(4.5, "D+", 1.5)]
    [InlineData(4.4, "D", 1.0)]
    [InlineData(4.0, "D", 1.0)]
    [InlineData(3.9, "F+", 0.5)]
    [InlineData(2.0, "F+", 0.5)]
    [InlineData(1.9, "F", 0.0)]
    [InlineData(0.0, "F", 0.0)]
    public void QuyDoiDiem_thang_10_sang_chu_va_thang_4(decimal diem10, string chu, decimal diem4)
    {
        var kq = QuyDoiDiem.TuThang10(diem10);

        Assert.Equal(chu, kq.DiemChu);
        Assert.Equal(diem4, kq.Diem4);
    }

    [Fact]
    public void QuyDoiDiem_lam_tron_truoc_khi_tra_thang_5_45_thanh_C()
    {
        var kq = QuyDoiDiem.TuThang10(5.45m);

        Assert.Equal(5.5m, kq.Diem10);
        Assert.Equal("C", kq.DiemChu);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(10.1)]
    public void QuyDoiDiem_ngoai_thang_10_bi_tu_choi(decimal diem)
    {
        var ex = Assert.Throws<DomainException>(() => QuyDoiDiem.TuThang10(diem));
        Assert.Equal("UPM_DIEM_NGOAI_THANG_10", ex.Code);
    }

    [Fact]
    public void QuyDoiDiem_thang_tuy_bien_thieu_muc_bi_tu_choi()
    {
        MucDiemChu[] thang = [new(5.0m, "P", 1.0m)];

        Assert.Equal("P", QuyDoiDiem.TuThang10(7m, thang).DiemChu);
        var ex = Assert.Throws<DomainException>(() => QuyDoiDiem.TuThang10(3m, thang));
        Assert.Equal("UPM_THANG_DIEM_KHONG_HOP_LE", ex.Code);
    }
}
