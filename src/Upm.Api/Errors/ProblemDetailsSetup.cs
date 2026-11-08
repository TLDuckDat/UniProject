using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace Upm.Api.Errors;

/// <summary>
/// Chuẩn hóa mọi lỗi thành ProblemDetails (RFC 9457) có thêm:
/// <list type="bullet">
/// <item><c>code</c>: mã lỗi nghiệp vụ/kỹ thuật (vd. <c>UPM_NOT_FOUND</c>, <c>UPM_A02_NO_QUA_10_TC</c>);</item>
/// <item><c>traceId</c>: để tra log Serilog.</item>
/// </list>
/// Tiêu đề mặc định bằng tiếng Việt.
/// </summary>
public static class ProblemDetailsSetup
{
    public const string CodeKey = "code";
    public const string TraceIdKey = "traceId";

    public static IServiceCollection AddUpmProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = Customize);
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    private static void Customize(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        var status = problem.Status ?? context.HttpContext.Response.StatusCode;

        problem.Instance ??= $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
        problem.Extensions.TryAdd(TraceIdKey,
            Activity.Current?.Id ?? context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity.Id ?? context.HttpContext.TraceIdentifier);

        var isValidation = problem is ValidationProblemDetails or HttpValidationProblemDetails;
        problem.Extensions.TryAdd(CodeKey, isValidation ? ErrorCodes.Validation : DefaultCode(status));

        // Thay tiêu đề tiếng Anh mặc định của ASP.NET Core bằng tiếng Việt.
        if (isValidation)
        {
            problem.Title = "Dữ liệu gửi lên không hợp lệ.";
        }
        else if (string.IsNullOrEmpty(problem.Title) || IsDefaultEnglishTitle(problem.Title))
        {
            problem.Title = DefaultTitle(status);
        }
    }

    private static string DefaultCode(int status) => status switch
    {
        StatusCodes.Status400BadRequest => ErrorCodes.BadRequest,
        StatusCodes.Status401Unauthorized => ErrorCodes.Unauthorized,
        StatusCodes.Status403Forbidden => ErrorCodes.Forbidden,
        StatusCodes.Status404NotFound => ErrorCodes.NotFound,
        StatusCodes.Status409Conflict => ErrorCodes.Conflict,
        >= 500 => ErrorCodes.Internal,
        _ => ErrorCodes.Unknown,
    };

    internal static string DefaultTitle(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Yêu cầu không hợp lệ.",
        StatusCodes.Status401Unauthorized => "Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn.",
        StatusCodes.Status403Forbidden => "Bạn không có quyền thực hiện thao tác này.",
        StatusCodes.Status404NotFound => "Không tìm thấy tài nguyên yêu cầu.",
        StatusCodes.Status405MethodNotAllowed => "Phương thức không được hỗ trợ.",
        StatusCodes.Status409Conflict => "Dữ liệu bị xung đột, vui lòng tải lại và thử lại.",
        StatusCodes.Status422UnprocessableEntity => "Vi phạm quy định nghiệp vụ.",
        >= 500 => "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.",
        _ => "Đã xảy ra lỗi.",
    };

    // Các title tiếng Anh mà ASP.NET Core tự điền (RFC reason phrase / "An error occurred...").
    private static bool IsDefaultEnglishTitle(string title) =>
        title.StartsWith("An error occurred", StringComparison.Ordinal)
        || title is "Bad Request" or "Unauthorized" or "Forbidden" or "Not Found" or "Method Not Allowed"
            or "Conflict" or "Unprocessable Entity" or "Internal Server Error" or "Service Unavailable";
}
