using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Upm.Domain.Common;

namespace Upm.Api.Errors;

/// <summary>
/// Chuyển mọi exception chưa bắt thành ProblemDetails.
/// <see cref="DomainException"/> → 422 kèm <c>code</c> nghiệp vụ; lỗi khác → 500 (không lộ chi tiết ra ngoài môi trường dev).
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
            case DomainException domain:
                logger.LogWarning("Vi phạm nghiệp vụ {Code}: {Message}", domain.Code, domain.Message);
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = ProblemDetailsSetup.DefaultTitle(StatusCodes.Status422UnprocessableEntity),
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
