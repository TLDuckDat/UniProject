using Upm.Domain.Common;
using Upm.Domain.Rules;

namespace Upm.Domain.Tests.Rules;

public sealed class RuleSetPolicyTests
{
    [Fact]
    public void RuleSet_da_duoc_dung_khong_the_sua()
    {
        var error = Assert.Throws<DomainException>(() => RuleSetPolicy.KiemTraCoTheSua(daSuDung: true));
        Assert.Equal("UPM_RULESET_DA_SU_DUNG", error.Code);
    }

    [Fact]
    public void RuleSet_chua_duoc_dung_co_the_sua() => RuleSetPolicy.KiemTraCoTheSua(daSuDung: false);
}
