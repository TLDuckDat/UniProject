namespace Upm.Domain.Common;

/// <summary>
/// Lỗi vi phạm luật nghiệp vụ. <see cref="Code"/> là mã nghiệp vụ trả về client
/// trong ProblemDetails (vd. <c>UPM_A02_NO_QUA_10_TC</c>); <see cref="Exception.Message"/>
/// là thông báo tiếng Việt có dấu hiển thị cho người dùng.
/// </summary>
public class DomainException : Exception
{
    public const string CodePrefix = "UPM_";

    public DomainException(string code, string message) : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (!code.StartsWith(CodePrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Mã lỗi nghiệp vụ phải bắt đầu bằng \"{CodePrefix}\".", nameof(code));
        }

        Code = code;
    }

    /// <summary>Mã lỗi nghiệp vụ, luôn bắt đầu bằng <c>UPM_</c>.</summary>
    public string Code { get; }

    /// <summary>
    /// Mã lỗi là "từ chối quyền" (không có quyền trên đối tượng, sai vai trò) → HTTP 403.
    /// Các mã khác là vi phạm luật nghiệp vụ → HTTP 422.
    /// </summary>
    public static bool LaLoiTuChoiQuyen(string code) =>
        code == "UPM_PERMISSION_DENIED"
        || code.StartsWith("UPM_CHI_", StringComparison.Ordinal)
        || code.EndsWith("_PERMISSION_DENIED", StringComparison.Ordinal);
}
