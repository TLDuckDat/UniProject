using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using Upm.Api.Errors;
using Upm.Infrastructure.Identity;

namespace Upm.Api.Authorization;

/// <summary>
/// Chống CSRF cho endpoint dùng cookie (refresh/logout): nếu request có header <c>Origin</c> thì Origin phải nằm trong
/// <c>XacThuc:AllowedOrigins</c> (rỗng → <c>Cors:AllowedOrigins</c>), sai → 403. Không có Origin → cho qua: trình duyệt
/// LUÔN gửi Origin với POST, nên request thiếu Origin không đến từ trang web của kẻ tấn công (và cũng không mang cookie
/// SameSite=Strict của nạn nhân).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class KiemTraOriginAttribute : Attribute, IAuthorizationFilter
{
    public const string Code = "UPM_CSRF_ORIGIN";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var origin = http.Request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin))
        {
            return;
        }

        var services = http.RequestServices;
        var allowed = services.GetRequiredService<IOptions<XacThucOptions>>().Value.AllowedOrigins;
        if (allowed.Length == 0)
        {
            allowed = services.GetRequiredService<IConfiguration>().GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        }

        if (allowed.Contains(origin.TrimEnd('/'), StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        var problem = services.GetRequiredService<ProblemDetailsFactory>().CreateProblemDetails(
            http, StatusCodes.Status403Forbidden, detail: "Yêu cầu không xuất phát từ trang web được phép.");
        problem.Extensions[ProblemDetailsSetup.CodeKey] = Code;
        context.Result = new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status403Forbidden,
            ContentTypes = { "application/problem+json" },
        };
    }
}
