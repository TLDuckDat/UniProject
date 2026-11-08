using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Upm.Application.PhanQuyen;

namespace Upm.Api.Authorization;

/// <summary>
/// Khai báo quyền chức năng cho controller/action: <c>[CoQuyen(Quyen.DotQuanLy)]</c>.
/// Nhiều quyền = chỉ cần CÓ MỘT. Quyền suy từ vai trò (BangQuyen) nạp từ DB mỗi request.
/// Phạm vi dữ liệu (cơ sở/khoa/bộ môn) KHÔNG kiểm ở đây – dùng IScopeFilter trong truy vấn.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class CoQuyenAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Quyen:";

    public CoQuyenAttribute(params string[] quyen)
    {
        if (quyen.Length == 0)
        {
            throw new ArgumentException("Phải khai báo ít nhất một quyền.", nameof(quyen));
        }
        Quyen = quyen;
        Policy = PolicyPrefix + string.Join(',', quyen);
    }

    public IReadOnlyList<string> Quyen { get; }
}

public sealed record QuyenRequirement(IReadOnlyList<string> Quyen) : IAuthorizationRequirement;

/// <summary>Kiểm người dùng có ít nhất một trong các quyền yêu cầu.</summary>
public sealed class QuyenHandler(IQuyenHienTai quyenHienTai) : AuthorizationHandler<QuyenRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, QuyenRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var quyen = await quyenHienTai.LayAsync();
        if (requirement.Quyen.Any(quyen.CoQuyen))
        {
            context.Succeed(requirement);
        }
    }
}

/// <summary>Sinh policy động từ tên "Quyen:A,B" – không phải đăng ký trước từng policy.</summary>
public sealed class QuyenPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(CoQuyenAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            return await base.GetPolicyAsync(policyName);
        }

        var quyen = policyName[CoQuyenAttribute.PolicyPrefix.Length..].Split(',', StringSplitOptions.RemoveEmptyEntries);
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new QuyenRequirement(quyen))
            .Build();
    }
}

public static class CoQuyenEndpointExtensions
{
    /// <summary>Cho minimal API: <c>app.MapGet(...).RequireQuyen(Quyen.X)</c>.</summary>
    public static TBuilder RequireQuyen<TBuilder>(this TBuilder builder, params string[] quyen)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(new CoQuyenAttribute(quyen));
}
