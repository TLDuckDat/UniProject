using Microsoft.AspNetCore.Mvc;
using Upm.Application.SystemInfo;

namespace Upm.Api.Controllers;

/// <summary>Kiểm tra kết nối API (không cần đăng nhập).</summary>
[ApiController]
[Route("api/v1/ping")]
[Tags("System")]
public sealed class PingController(ISystemInfoService systemInfo) : ControllerBase
{
    /// <summary>Trả về "pong", phiên bản ứng dụng và thời gian máy chủ.</summary>
    [HttpGet(Name = "ping")]
    [ProducesResponseType<PingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public ActionResult<PingResponse> Get() => Ok(systemInfo.Ping());
}
