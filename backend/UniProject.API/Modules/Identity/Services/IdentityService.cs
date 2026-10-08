using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UniProject.Share.Application.Interfaces;
using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Constants;
using UniProject.Share.Infrastructure.Exceptions;
using UniProject.Share.Infrastructure.Extensions;
using UniProject.API.Common;
using UniProject.API.Data;

namespace UniProject.API.Modules.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly AppDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly ILogger<IdentityService> _logger;
        private readonly CurrentActor _actor;

        public IdentityService(
            AppDbContext dbContext,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            ILogger<IdentityService> logger,
            CurrentActor actor)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _logger = logger;
            _actor = actor;
        }

        public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var username = request.Username.Trim().ToLowerInvariant();
            var user = await _dbContext.TaiKhoans
                .Include(u => u.VaiTro)
                .FirstOrDefaultAsync(u => u.TenDangNhap == username, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Login failed for user {Username}: User not found", request.Username);
                throw new UnauthorizedException("Tên đăng nhập hoặc mật khẩu không chính xác.");
            }

            bool isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.MatKhauHash);

            if (!isPasswordValid)
            {
                _logger.LogWarning("Login failed for user {Username}: Invalid password", request.Username);
                throw new UnauthorizedException("Tên đăng nhập hoặc mật khẩu không chính xác.");
            }

            if (user.DaKhoa)
            {
                _logger.LogWarning("Login failed for user {Username}: Account is locked", request.Username);
                throw new ForbiddenException("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
            }

            var (accessToken, accessExpiry) = _jwtTokenGenerator.GenerateAccessToken(user, user.VaiTro.TenVaiTro);
            var (refreshToken, refreshExpiry) = _jwtTokenGenerator.GenerateRefreshToken();

            // Tài khoản seed được băm với work factor thấp cho nhanh, nâng lên mức chuẩn ở lần đăng nhập đầu
            if (_passwordHasher.NeedsRehash(user.MatKhauHash))
                user.UpdatePassword(_passwordHasher.HashPassword(request.Password));

            user.SetRefreshToken(refreshToken, refreshExpiry);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {Username} logged in successfully with role {Role}", user.TenDangNhap, user.VaiTro.TenVaiTro);

            var response = new AuthResponse(
                user.Id,
                user.TenDangNhap,
                user.HoTen,
                user.Email,
                user.VaiTro.TenVaiTro,
                accessToken,
                refreshToken,
                refreshExpiry);

            return Result<AuthResponse>.Ok(response, "Đăng nhập thành công.");
        }

        /// <summary>
        /// Refresh token được xoay vòng: mỗi lần làm mới sẽ thay bằng token mới, token cũ hết hiệu lực.
        /// </summary>
        public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            var principal = _jwtTokenGenerator.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
            {
                throw new UnauthorizedException("Access token không hợp lệ.");
            }

            var userIdClaim = principal.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedException("Access token không chứa thông tin người dùng hợp lệ.");
            }

            var user = await _dbContext.TaiKhoans
                .Include(u => u.VaiTro)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new UnauthorizedException("Refresh token không hợp lệ hoặc đã hết hạn.");
            }

            if (user.DaKhoa)
            {
                throw new ForbiddenException("Tài khoản đã bị khóa.");
            }

            var (newAccessToken, _) = _jwtTokenGenerator.GenerateAccessToken(user, user.VaiTro.TenVaiTro);
            var (newRefreshToken, newRefreshExpiry) = _jwtTokenGenerator.GenerateRefreshToken();

            user.SetRefreshToken(newRefreshToken, newRefreshExpiry);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var response = new AuthResponse(
                user.Id,
                user.TenDangNhap,
                user.HoTen,
                user.Email,
                user.VaiTro.TenVaiTro,
                newAccessToken,
                newRefreshToken,
                newRefreshExpiry);

            return Result<AuthResponse>.Ok(response, "Làm mới token thành công.");
        }

        /// <summary>
        /// Thu hồi refresh token để các phiên đăng nhập khác phải đăng nhập lại.
        /// </summary>
        public async Task<Result> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.TaiKhoans.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException("Tài khoản", userId);
            }

            if (!_passwordHasher.VerifyPassword(request.OldPassword, user.MatKhauHash))
            {
                throw new ValidationException("OldPassword", "Mật khẩu cũ không chính xác.");
            }

            var newHash = _passwordHasher.HashPassword(request.NewPassword);
            user.UpdatePassword(newHash);
            user.RevokeRefreshToken();

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Password changed successfully for user ID {UserId}", userId);

            return Result.Ok("Đổi mật khẩu thành công. Vui lòng đăng nhập lại.");
        }

        public async Task<Result> ResetPasswordAsync(int userId, ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.TaiKhoans.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException("Tài khoản", userId);
            }

            var newHash = _passwordHasher.HashPassword(request.NewPassword);
            user.UpdatePassword(newHash);
            user.RevokeRefreshToken();

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Password reset by admin for user ID {UserId}", userId);

            return Result.Ok("Đặt lại mật khẩu thành công.");
        }

        public async Task<Result<UserDto>> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken = default)
        {
            var username = request.Username.Trim().ToLowerInvariant();
            var email = request.Email.Trim().ToLowerInvariant();

            if (await _dbContext.TaiKhoans.AnyAsync(u => u.TenDangNhap == username, cancellationToken))
            {
                throw new ConflictException($"Tên đăng nhập '{request.Username}' đã tồn tại.");
            }

            if (await _dbContext.TaiKhoans.AnyAsync(u => u.Email == email, cancellationToken))
            {
                throw new ConflictException($"Email '{request.Email}' đã được sử dụng.");
            }

            var role = await _dbContext.VaiTros.FirstOrDefaultAsync(r => r.TenVaiTro == request.Role, cancellationToken);
            if (role == null)
            {
                throw new NotFoundException($"Không tìm thấy vai trò '{request.Role}'.");
            }

            var hash = _passwordHasher.HashPassword(request.Password);
            var user = new TaiKhoan(username, hash, email, request.FullName, role.Id);

            _dbContext.TaiKhoans.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created new account for {Username} with role {Role}", user.TenDangNhap, role.TenVaiTro);

            var dto = new UserDto(
                user.Id,
                user.TenDangNhap,
                user.HoTen,
                user.Email,
                role.TenVaiTro,
                user.DaKhoa,
                user.LanDangNhapCuoi,
                user.CreatedAt);

            return Result<UserDto>.Ok(dto, "Tạo tài khoản thành công.");
        }

        /// <summary>
        /// Thu hồi refresh token để phiên hiện tại không thể làm mới sau khi bị khóa.
        /// </summary>
        public async Task<Result> LockAccountAsync(int userId, CancellationToken cancellationToken = default)
        {
            if (userId == _actor.AccountId)
                throw new BusinessRuleException("Không thể tự khóa tài khoản của chính mình.");

            var user = await _dbContext.TaiKhoans.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException("Tài khoản", userId);
            }

            user.Lock();
            user.RevokeRefreshToken();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Account locked for user ID {UserId}", userId);
            return Result.Ok("Khóa tài khoản thành công.");
        }

        public async Task<Result> UnlockAccountAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.TaiKhoans.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException("Tài khoản", userId);
            }

            user.Unlock();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Account unlocked for user ID {UserId}", userId);
            return Result.Ok("Mở khóa tài khoản thành công.");
        }

        /// <summary>
        /// Thu hồi refresh token để người dùng phải đăng nhập lại và nhận token mang vai trò mới.
        /// </summary>
        public async Task<Result> AssignRoleAsync(int userId, AssignRoleRequest request, CancellationToken cancellationToken = default)
        {
            if (userId == _actor.AccountId)
                throw new BusinessRuleException("Không thể tự thay đổi vai trò của chính mình.");

            var user = await _dbContext.TaiKhoans.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException("Tài khoản", userId);
            }

            var role = await _dbContext.VaiTros.FirstOrDefaultAsync(r => r.TenVaiTro == request.Role, cancellationToken);
            if (role == null)
            {
                throw new NotFoundException($"Không tìm thấy vai trò '{request.Role}'.");
            }

            user.AssignRole(role.Id);
            user.RevokeRefreshToken();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Assigned role {Role} to user ID {UserId}", role.TenVaiTro, userId);
            return Result.Ok("Cập nhật vai trò thành công.");
        }

        public async Task<Result<UserDto>> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.TaiKhoans
                .AsNoTracking()
                .Include(u => u.VaiTro)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                throw new NotFoundException("Tài khoản", userId);
            }

            var dto = new UserDto(
                user.Id,
                user.TenDangNhap,
                user.HoTen,
                user.Email,
                user.VaiTro.TenVaiTro,
                user.DaKhoa,
                user.LanDangNhapCuoi,
                user.CreatedAt);

            return Result<UserDto>.Ok(dto);
        }

        public async Task<PagedResult<UserDto>> GetUsersAsync(PaginationParams @params, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.TaiKhoans
                .AsNoTracking()
                .Include(u => u.VaiTro)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(@params.Search))
            {
                var search = @params.Search.Trim().ToLower();
                query = query.Where(u => u.TenDangNhap.ToLower().Contains(search) ||
                                         u.HoTen.ToLower().Contains(search) ||
                                         u.Email.ToLower().Contains(search));
            }

            query = @params.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(u => u.CreatedAt)
                : query.OrderBy(u => u.CreatedAt);

            return await query.ToPagedResultAsync(
                @params.PageNumber,
                @params.PageSize,
                u => new UserDto(
                    u.Id,
                    u.TenDangNhap,
                    u.HoTen,
                    u.Email,
                    u.VaiTro.TenVaiTro,
                    u.DaKhoa,
                    u.LanDangNhapCuoi,
                    u.CreatedAt),
                cancellationToken);
        }
    }
}
