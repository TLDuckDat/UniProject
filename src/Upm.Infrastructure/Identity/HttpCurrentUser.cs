using System.Globalization;
using Microsoft.AspNetCore.Http;
using Upm.Application.Abstractions;

namespace Upm.Infrastructure.Identity;

/// <summary>
/// Đọc người dùng từ access token của request hiện tại. Không có HttpContext (job nền, seeder, test)
/// → coi như hệ thống (mọi giá trị null).
/// </summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public const string CoSoHeader = "X-CoSo-Id";

    private HttpContext? Context => accessor.HttpContext;

    public bool DaDangNhap => Context?.User.Identity?.IsAuthenticated == true;

    public long? TaiKhoanId => DocSo(UpmClaims.Sub);

    public string? TenDangNhap => DaDangNhap ? Context!.User.FindFirst(UpmClaims.TenDangNhap)?.Value : null;

    public long? GiangVienId => DocSo(UpmClaims.GiangVienId);

    public long? SinhVienId => DocSo(UpmClaims.SinhVienId);

    public long? CoSoDangChonId =>
        Context?.Request.Headers.TryGetValue(CoSoHeader, out var v) == true
        && long.TryParse(v.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();

    private long? DocSo(string claim) =>
        DaDangNhap && long.TryParse(Context!.User.FindFirst(claim)?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;
}
