using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Exceptions;

namespace UniProject.API.Common
{
    /// <summary>
    /// Chuyển exception thành response JSON dạng <see cref="Result"/> với mã HTTP phù hợp,
    /// nhờ vậy service chỉ cần throw NotFoundException, ConflictException, ... thay vì tự dựng response lỗi.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (statusCode, result) = exception switch
            {
                ValidationException ex => (HttpStatusCode.BadRequest, Result.Fail(ex.Message, ex.Errors)),
                NotFoundException ex => (HttpStatusCode.NotFound, Result.Fail(ex.Message)),
                ConflictException ex => (HttpStatusCode.Conflict, Result.Fail(ex.Message)),
                UnauthorizedException ex => (HttpStatusCode.Unauthorized, Result.Fail(ex.Message)),
                ForbiddenException ex => (HttpStatusCode.Forbidden, Result.Fail(ex.Message)),
                BusinessRuleException ex => (HttpStatusCode.BadRequest, Result.Fail(ex.Message)),
                DomainException ex => (HttpStatusCode.BadRequest, Result.Fail(ex.Message)),
                // Mất kết nối SQL Server: báo 503 để client biết là lỗi tạm thời, thử lại sau.
                Exception ex when DatabaseErrors.IsConnectionFailure(ex) => (HttpStatusCode.ServiceUnavailable, Result.Fail("Không kết nối được tới cơ sở dữ liệu. Vui lòng thử lại sau hoặc liên hệ quản trị viên.")),
                // Hai request ghi cùng lúc: một bên vi phạm unique index hoặc RowVersion → báo xung đột thay vì 500.
                DbUpdateConcurrencyException => (HttpStatusCode.Conflict, Result.Fail("Dữ liệu đã bị người khác thay đổi. Vui lòng tải lại và thử lại.")),
                DbUpdateException => (HttpStatusCode.Conflict, Result.Fail("Dữ liệu bị trùng hoặc vi phạm ràng buộc. Vui lòng kiểm tra lại.")),
                _ => (HttpStatusCode.InternalServerError, Result.Fail("Đã có lỗi xảy ra trên hệ thống. Vui lòng thử lại sau."))
            };

            if (statusCode == HttpStatusCode.ServiceUnavailable)
                _logger.LogError(exception, "Mất kết nối tới SQL Server: {Message}", exception.Message);
            else if (statusCode == HttpStatusCode.InternalServerError)
                _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
            else
                _logger.LogInformation("Request failed with {StatusCode}: {Message}", (int)statusCode, exception.Message);

            httpContext.Response.StatusCode = (int)statusCode;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(JsonSerializer.Serialize(result, JsonOptions), cancellationToken);

            return true;
        }
    }
}
