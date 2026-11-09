using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Upm.Application.XacThuc;
using Upm.Domain.Common;

namespace Upm.Api.Errors;

/// <summary>
/// Chuyển mọi exception chưa bắt thành ProblemDetails.
/// <see cref="DomainException"/> → 422 kèm <c>code</c> nghiệp vụ, hoặc 403 nếu là mã từ chối quyền (<see cref="DomainException.LaLoiTuChoiQuyen"/>); lỗi khác → 500 (không lộ chi tiết ra ngoài môi trường dev).
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        switch (exception)
        {
            case TamKhoaDangNhapException khoa:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = ProblemDetailsSetup.DefaultTitle(StatusCodes.Status429TooManyRequests),
                    Detail = khoa.Message,
                };
                problem.Extensions[ProblemDetailsSetup.CodeKey] = khoa.Code;
                httpContext.Response.Headers.RetryAfter =
                    ((int)Math.Ceiling(khoa.ThuLaiSau.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                break;

            case XacThucException auth:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = ProblemDetailsSetup.DefaultTitle(StatusCodes.Status401Unauthorized),
                    Detail = auth.Message,
                };
                problem.Extensions[ProblemDetailsSetup.CodeKey] = auth.Code;
                break;

            case DomainException domain:
                var status = DomainException.LaLoiTuChoiQuyen(domain.Code)
                    ? StatusCodes.Status403Forbidden
                    : StatusCodes.Status422UnprocessableEntity;
                logger.LogWarning("Vi phạm nghiệp vụ {Code} ({Status}): {Message}", domain.Code, status, domain.Message);
                problem = new ProblemDetails
                {
                    Status = status,
                    Title = ProblemDetailsSetup.DefaultTitle(status),
                    Detail = domain.Message,
                };
                problem.Extensions[ProblemDetailsSetup.CodeKey] = domain.Code;
                break;

            case BadHttpRequestException badRequest:
                problem = new ProblemDetails
                {
                    Status = badRequest.StatusCode,
                    Detail = environment.IsDevelopment() ? badRequest.Message : null,
                };
                break;

            default:
                logger.LogError(exception, "Lỗi chưa xử lý khi gọi {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = environment.IsDevelopment() ? exception.Message : null,
                };
                break;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
