using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Upm.Application.Abstractions;
using Upm.Domain.Entities;

namespace Upm.Infrastructure.Identity;

/// <summary>Phát hành access token (JWT HS256) và sinh/băm refresh token.</summary>
public sealed class TokenService(IOptions<JwtOptions> options, IClock clock)
{
    private readonly JwtOptions _options = options.Value;

    public static SymmetricSecurityKey TaoKhoaKy(string signingKey)
    {
        var bytes = Encoding.UTF8.GetBytes(signingKey);
        if (bytes.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey phải dài tối thiểu 32 byte.");
        }
        return new SymmetricSecurityKey(bytes);
    }

    public (string Token, DateTimeOffset HetHanLuc) TaoAccessToken(TaiKhoan taiKhoan)
    {
        var now = clock.Now;
        var hetHan = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(UpmClaims.Sub, taiKhoan.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new(UpmClaims.TenDangNhap, taiKhoan.TenDangNhap),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        if (taiKhoan.GiangVienId is { } gv)
        {
            claims.Add(new Claim(UpmClaims.GiangVienId, gv.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }
        if (taiKhoan.SinhVienId is { } sv)
        {
            claims.Add(new Claim(UpmClaims.SinhVienId, sv.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = hetHan.UtcDateTime,
            SigningCredentials = new SigningCredentials(TaoKhoaKy(_options.SigningKey), SecurityAlgorithms.HmacSha256),
        });
        return (token, hetHan);
    }

    /// <summary>Refresh token ngẫu nhiên 256 bit (base64url) – entropy cao nên băm SHA-256 là đủ để lưu.</summary>
    public static string TaoRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    public static string BamRefreshToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
