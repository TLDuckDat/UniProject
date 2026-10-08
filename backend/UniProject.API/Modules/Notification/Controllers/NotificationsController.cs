using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Constants;
using UniProject.Share.Infrastructure.Exceptions;

namespace UniProject.API.Modules.Notification
{
    [ApiController]
    [Route("api/v1/notifications")]
    [Tags("Notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationsController(INotificationService service)
        {
            _service = service;
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.GiangVien}")]
        [HttpPost]
        [ProducesResponseType(typeof(Result<int>), 201)]
        public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request, CancellationToken ct)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _service.CreateNotificationAsync(currentUserId, request, ct);
            return StatusCode(201, result);
        }

        [Authorize]
        [HttpGet("my")]
        [ProducesResponseType(typeof(PagedResult<NotificationDto>), 200)]
        public async Task<IActionResult> GetMy([FromQuery] PaginationParams @params, [FromQuery] bool? isRead, CancellationToken ct)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _service.GetMyNotificationsAsync(currentUserId, @params, isRead, ct);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(Result<int>), 200)]
        public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
        {
            var currentUserId = GetCurrentUserId();
            var count = await _service.GetUnreadCountAsync(currentUserId, ct);
            return Ok(Result<int>.Ok(count));
        }

        [Authorize]
        [HttpPatch("{id:int}/read")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> MarkAsRead(int id, CancellationToken ct)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _service.MarkAsReadAsync(id, currentUserId, ct);
            return Ok(result);
        }

        [Authorize]
        [HttpPatch("mark-all-read")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _service.MarkAllAsReadAsync(currentUserId, ct);
            return Ok(result);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                throw new UnauthorizedException("Không thể xác định thông tin người dùng.");
            return userId;
        }
    }
}
