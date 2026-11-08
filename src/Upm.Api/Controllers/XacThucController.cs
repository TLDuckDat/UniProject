using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Upm.Api.Authorization;
using Upm.Application.Abstractions;
using Upm.Application.XacThuc;
using Upm.Infrastructure.Identity;

namespace Upm.Api.Controllers;

/// <summary>
/// Đăng nhập / làm mới / đăng xuất. Access token trả trong body (client giữ trong bộ nhớ);
/// refresh token chỉ đi qua cookie HttpOnly, SameSite=Strict, Path=/api/v1/auth.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Tags("XacThuc")]
public sealed class XacThucController(IXacThucService xacThuc, ICurrentUser currentUser, IOptions<JwtOptions> jwt) : ControllerBase
{
    public const string RefreshCookie = "upm_rt";
    public const string CookiePath = "/api/v1/auth";
    public const string DangNhapRateLimit = "dang-nhap";

    /// <summary>Đăng nhập bằng tên đăng nhập + mật khẩu.</summary>
    [HttpPost("login", Name = "dangNhap")]
    [AllowAnonymous]
    [EnableRateLimiting(DangNhapRateLimit)]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<TokenResponse>> DangNhap(DangNhapRequest request, CancellationToken ct)
    {
        var phien = await xacThuc.DangNhapAsync(request, ct);
        GhiCookie(phien);
        return Ok(phien.Token);
    }

    /// <summary>Cấp access token mới từ refresh token trong cookie (xoay vòng refresh token).</summary>
    [HttpPost("refresh", Name = "lamMoi")]
    [KiemTraOrigin]
    [AllowAnonymous]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<TokenResponse>> LamMoi(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookie, out var token) || string.IsNullOrEmpty(token))
        {
            throw new XacThucException(XacThucService.RefreshKhongHopLe, "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
        }

        try
        {
            var phien = await xacThuc.LamMoiAsync(token, ct);
            GhiCookie(phien);
            return Ok(phien.Token);
        }
        catch (XacThucException)
        {
            XoaCookie();
            throw;
        }
    }

    /// <summary>Thu hồi refresh token hiện tại và xóa cookie.</summary>
    [HttpPost("logout", Name = "dangXuat")]
    [KiemTraOrigin]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DangXuat(CancellationToken ct)
    {
        Request.Cookies.TryGetValue(RefreshCookie, out var token);
        await xacThuc.DangXuatAsync(token, ct);
        XoaCookie();
        return NoContent();
    }

    /// <summary>Thông tin người dùng hiện tại: vai trò + phạm vi, quyền, các cơ sở được làm việc.</summary>
    [HttpGet("me", Name = "toi")]
    [Authorize]
    [ProducesResponseType<NguoiDungResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NguoiDungResponse>> Toi(CancellationToken ct) =>
        Ok(await xacThuc.ThongTinAsync(currentUser.TaiKhoanId!.Value, ct));

    private void GhiCookie(PhienDangNhap phien) =>
        Response.Cookies.Append(RefreshCookie, phien.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = jwt.Value.RefreshCookieSecure,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            Expires = phien.RefreshHetHanLuc,
            IsEssential = true,
        });

    private void XoaCookie() =>
        Response.Cookies.Delete(RefreshCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure = jwt.Value.RefreshCookieSecure,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
        });
}
