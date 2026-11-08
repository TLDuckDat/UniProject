using Upm.Domain.Common;

namespace Upm.Domain.Tests.Common;

public class DomainExceptionTests
{
    [Fact]
    public void Giu_nguyen_ma_loi_va_thong_bao_tieng_Viet()
    {
        var ex = new DomainException("UPM_A02_NO_QUA_10_TC", "Sinh viên nợ quá 10 tín chỉ.");

        Assert.Equal("UPM_A02_NO_QUA_10_TC", ex.Code);
        Assert.Equal("Sinh viên nợ quá 10 tín chỉ.", ex.Message);
    }

    [Theory]
    [InlineData("A02_NO_QUA_10_TC")]
    [InlineData("upm_a02")]
    public void Ma_loi_khong_bat_dau_bang_UPM_bi_tu_choi(string code)
    {
        Assert.Throws<ArgumentException>(() => new DomainException(code, "Thông báo"));
    }

    [Theory]
    [InlineData("", "Thông báo")]
    [InlineData("UPM_X", " ")]
    public void Ma_hoac_thong_bao_rong_bi_tu_choi(string code, string message)
    {
        Assert.ThrowsAny<ArgumentException>(() => new DomainException(code, message));
    }
}
