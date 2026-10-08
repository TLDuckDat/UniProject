using System.Security.Claims;
using UniProject.Share.Application.Interfaces;

namespace UniProject.API.Common
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        /// <summary>
        /// Mã tài khoản (TaiKhoan.Id), không phải mã sinh viên / giảng viên.
        /// </summary>
        public int? UserID
        {
            get
            {
                var id = User?.FindFirst("UserId")?.Value ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(id, out var userId) ? userId : null;
            }
        }

        public string? Username => User?.Identity?.Name;

        public IReadOnlyList<string> Roles => User?
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList() ?? (IReadOnlyList<string>)Array.Empty<string>();

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

        public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
    }
}
