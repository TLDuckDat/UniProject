using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace UniProject.API.Modules.Identity
{
    public interface IJwtTokenGenerator
    {
        (string Token, DateTime ExpiryTime) GenerateAccessToken(TaiKhoan user, string roleName);
        (string RefreshToken, DateTime ExpiryTime) GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }

    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _configuration;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Claim "UserId" được CurrentUser và AuthController dùng để xác định người đang đăng nhập.
        /// </summary>
        public (string Token, DateTime ExpiryTime) GenerateAccessToken(TaiKhoan user, string roleName)
        {
            var keyString = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Jwt__Key trong file .env.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var mins) ? mins : 120;
            var expiryTime = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.TenDangNhap),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.GivenName, user.HoTen),
                new(ClaimTypes.Role, roleName),
                new("UserId", user.Id.ToString()),
                new("Role", roleName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "QLDoAn",
                audience: _configuration["Jwt:Audience"] ?? "QLDoAn",
                claims: claims,
                expires: expiryTime,
                signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiryTime);
        }

        public (string RefreshToken, DateTime ExpiryTime) GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            var refreshToken = Convert.ToBase64String(randomNumber);

            var days = int.TryParse(_configuration["Jwt:RefreshTokenExpiryDays"], out var d) ? d : 7;
            var expiryTime = DateTime.UtcNow.AddDays(days);

            return (refreshToken, expiryTime);
        }

        /// <summary>
        /// Dùng khi refresh token: bỏ qua hạn dùng nhưng vẫn kiểm tra chữ ký và thuật toán,
        /// để không chấp nhận token giả mạo.
        /// </summary>
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var keyString = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Jwt__Key trong file .env.");
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString)),
                ValidateLifetime = false
            };

            ClaimsPrincipal principal;
            SecurityToken securityToken;
            try
            {
                principal = new JwtSecurityTokenHandler().ValidateToken(token, tokenValidationParameters, out securityToken);
            }
            catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
            {
                return null; // token sai định dạng / sai chữ ký → service trả 401
            }

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
    }
}
