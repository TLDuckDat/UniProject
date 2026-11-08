namespace Upm.Infrastructure.Identity;

/// <summary>Cấu hình mục "Jwt" trong appsettings.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "upm-api";
    public string Audience { get; set; } = "upm-web";

    /// <summary>Khóa ký HMAC-SHA256, tối thiểu 32 byte. KHÔNG commit khóa production – dùng user-secrets / biến môi trường.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;

    /// <summary>Cookie refresh token có cờ Secure (mặc định true; tắt chỉ khi test qua HTTP thuần).</summary>
    public bool RefreshCookieSecure { get; set; } = true;
}

/// <summary>Tên claim trong access token (giữ ngắn, không map sang tên dài của Microsoft).</summary>
public static class UpmClaims
{
    public const string Sub = "sub";
    public const string TenDangNhap = "name";
    public const string GiangVienId = "gv";
    public const string SinhVienId = "sv";
}
