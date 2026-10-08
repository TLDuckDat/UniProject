using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Constants;
using UniProject.Share.Infrastructure.Exceptions;

namespace UniProject.API.Modules.Identity
{
    [ApiController]
    [Route("api/v1/auth")]
    [Tags("Auth")]
    public class AuthController : ControllerBase
    {
        private readonly IIdentityService _identityService;

        public AuthController(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        [AllowAnonymous]
        [EnableRateLimiting("login")]
        [HttpPost("login")]
        [ProducesResponseType(typeof(Result<AuthResponse>), 200)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var result = await _identityService.LoginAsync(request, cancellationToken);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(Result<AuthResponse>), 200)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            var result = await _identityService.RefreshTokenAsync(request, cancellationToken);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _identityService.ChangePasswordAsync(userId, request, cancellationToken);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(Result<UserDto>), 200)]
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _identityService.GetCurrentUserAsync(userId, cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost("users")]
        [ProducesResponseType(typeof(Result<UserDto>), 201)]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
        {
            var result = await _identityService.CreateAccountAsync(request, cancellationToken);
            return StatusCode(201, result);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpGet("users")]
        [ProducesResponseType(typeof(PagedResult<UserDto>), 200)]
        public async Task<IActionResult> GetUsers([FromQuery] PaginationParams @params, CancellationToken cancellationToken)
        {
            var result = await _identityService.GetUsersAsync(@params, cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("users/{id:int}/lock")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> LockAccount(int id, CancellationToken cancellationToken)
        {
            var result = await _identityService.LockAccountAsync(id, cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("users/{id:int}/unlock")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> UnlockAccount(int id, CancellationToken cancellationToken)
        {
            var result = await _identityService.UnlockAccountAsync(id, cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("users/{id:int}/reset-password")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            var result = await _identityService.ResetPasswordAsync(id, request, cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("users/{id:int}/role")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> AssignRole(int id, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
        {
            var result = await _identityService.AssignRoleAsync(id, request, cancellationToken);
            return Ok(result);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedException("Không thể xác thực thông tin người dùng hiện tại.");
            }
            return userId;
        }
    }
}
