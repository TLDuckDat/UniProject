using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;

namespace UniProject.API.Modules.Identity
{
    public interface IIdentityService
    {
        Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
        Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
        Task<Result> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
        Task<Result> ResetPasswordAsync(int userId, ResetPasswordRequest request, CancellationToken cancellationToken = default);
        Task<Result<UserDto>> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken = default);
        Task<Result> LockAccountAsync(int userId, CancellationToken cancellationToken = default);
        Task<Result> UnlockAccountAsync(int userId, CancellationToken cancellationToken = default);
        Task<Result> AssignRoleAsync(int userId, AssignRoleRequest request, CancellationToken cancellationToken = default);
        Task<Result<UserDto>> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);
        Task<PagedResult<UserDto>> GetUsersAsync(PaginationParams @params, CancellationToken cancellationToken = default);
    }
}
