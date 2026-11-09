using Upm.Domain.Common;

namespace Upm.Domain.Rules;

/// <summary>Quy tắc bất biến cho phiên bản quy chế đã được dùng để xét.</summary>
public static class RuleSetPolicy
{
    public static void KiemTraCoTheSua(bool daSuDung)
    {
        if (daSuDung)
            throw new DomainException("UPM_RULESET_DA_SU_DUNG", "RuleSet đã được dùng để xét và đã khóa; hãy nhân bản để tạo phiên bản mới.");
    }
}
