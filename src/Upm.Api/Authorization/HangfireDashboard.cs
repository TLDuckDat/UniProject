using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Upm.Application.PhanQuyen;
using Upm.Domain.PhanQuyen;
using Upm.Infrastructure.Identity;

namespace Upm.Api.Authorization;

/// <summary>
/// Dashboard Hangfire (/hangfire) dùng lại xác thực JWT hiện có, chỉ vai trò QuanTri được vào.
/// Trình duyệt không tự gửi Bearer khi mở trang, nên: mở lần đầu bằng <c>/hangfire?access_token=…</c> → token được
/// chuyển vào cookie HttpOnly <see cref="Cookie"/> (Path=/hangfire, SameSite=Strict, hết hạn cùng token) cho các
/// request sau của dashboard. Token trong query KHÔNG bị ghi log (Serilog chỉ log path).
/// </summary>
public static class HangfireDashboard
{
    public const string Path = "/hangfire";
    public const string Cookie = "upm_hf";
    private const string TuQuery = "upm_hf_tu_query";

    /// <summary>Gắn vào JwtBearerEvents: đọc token từ query/cookie cho riêng đường dẫn dashboard.</summary>
    public static void CauHinhJwt(JwtBearerEvents events)
    {
        events.OnMessageReceived = ctx =>
        {
            if (!ctx.Request.Path.StartsWithSegments(Path) || !string.IsNullOrEmpty(ctx.Token))
            {
                return Task.CompletedTask;
            }

            if (ctx.Request.Query.TryGetValue("access_token", out var q) && !string.IsNullOrEmpty(q))
            {
                ctx.Token = q.ToString();
                ctx.HttpContext.Items[TuQuery] = true;
            }
            else if (ctx.Request.Cookies.TryGetValue(Cookie, out var c))
            {
                ctx.Token = c;
            }
            return Task.CompletedTask;
        };

        events.OnTokenValidated = ctx =>
        {
            if (ctx.HttpContext.Items.ContainsKey(TuQuery) && ctx.SecurityToken is { } token)
            {
                var jwt = ctx.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtOptions>>().Value;
                ctx.Response.Cookies.Append(Cookie, ctx.Request.Query["access_token"].ToString(), new CookieOptions
                {
                    HttpOnly = true,
                    Secure = jwt.RefreshCookieSecure,
                    SameSite = SameSiteMode.Strict,
                    Path = Path,
                    Expires = token.ValidTo,
                    IsEssential = true,
                });
            }
            return Task.CompletedTask;
        };
    }
}

/// <summary>Chỉ người dùng đã đăng nhập có vai trò QuanTri.</summary>
public sealed class HangfireQuanTriFilter : IDashboardAsyncAuthorizationFilter
{
    public async Task<bool> AuthorizeAsync(DashboardContext context)
    {
        var http = context.GetHttpContext();
        if (http.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var quyen = await http.RequestServices.GetRequiredService<IQuyenHienTai>().LayAsync(http.RequestAborted);
        return quyen.CoVaiTro(VaiTroMa.QuanTri);
    }
}
