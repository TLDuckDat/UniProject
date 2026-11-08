using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Upm.Api.IntegrationTests.Infrastructure;
using Upm.Application.XacThuc;

namespace Upm.Api.IntegrationTests.XacThuc;

[Collection(SharedApiFixture.Name)]
public sealed class XacThucTests(UpmApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Dang_nhap_dung_tra_access_token_va_cookie_refresh_HttpOnly()
    {
        await factory.SeedAsync(Ct);
        var client = factory.CreateRawClient();

        var res = await client.PostAsJsonAsync("/api/v1/auth/login", new DangNhapRequest("pdt.hn", "Upm@123456"), Ct);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var token = await res.Content.ReadFromJsonAsync<TokenResponse>(Ct);
        Assert.False(string.IsNullOrEmpty(token!.AccessToken));
        Assert.InRange(token.HetHanLuc - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(16)); // 15 phút
        Assert.Contains(token.NguoiDung.VaiTro, v => v is { MaVaiTro: "PhongDaoTao", MaCoSo: "HN" });

        var cookie = Assert.Single(res.Headers.GetValues("Set-Cookie"), c => c.StartsWith("upm_rt=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/v1/auth", cookie, StringComparison.OrdinalIgnoreCase);
        // Refresh token KHÔNG nằm trong body.
        Assert.DoesNotContain(UpmApiFactory.LayRefreshCookie(res)!, await res.Content.ReadAsStringAsync(Ct), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("pdt.hn", "SaiMatKhau")]
    [InlineData("khong.ton.tai", "Upm@123456")]
    public async Task Sai_thong_tin_tra_401_cung_mot_thong_bao(string ten, string matKhau)
    {
        await factory.SeedAsync(Ct);

        var res = await factory.CreateRawClient().PostAsJsonAsync("/api/v1/auth/login", new DangNhapRequest(ten, matKhau), Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        using var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync(Ct));
        Assert.Equal("UPM_AUTH_SAI_THONG_TIN", json.RootElement.GetProperty("code").GetString());
        Assert.Equal("Tên đăng nhập hoặc mật khẩu không đúng.", json.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Goi_API_can_dang_nhap_khi_chua_co_token_tra_401_ProblemDetails()
    {
        var res = await factory.CreateRawClient().GetAsync("/api/v1/auth/me", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Me_tra_vai_tro_pham_vi_quyen_va_co_so()
    {
        var (client, _, _) = await factory.DangNhapAsync("pdt.2coso", Ct);

        var me = await client.GetFromJsonAsync<NguoiDungResponse>("/api/v1/auth/me", Ct);

        Assert.Equal("pdt.2coso", me!.TenDangNhap);
        Assert.Equal(["HCM", "HN"], me.CoSoDuocPhep.Select(c => c.MaCoSo).Order());
        Assert.Contains("Dot.QuanLy", me.Quyen);
        Assert.Equal(2, me.VaiTro.Count);
    }

    [Fact]
    public async Task Access_token_het_han_bi_401_va_refresh_cap_token_moi_dung_duoc()
    {
        var (_, token, refresh) = await factory.DangNhapAsync("gv001", Ct);
        var client = factory.CreateRawClient();

        // 1. Access token đã hết hạn (ký đúng khóa, cùng tài khoản) → 401.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TaoTokenHetHan(token.NguoiDung.Id));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me", Ct)).StatusCode);

        // 2. Refresh bằng cookie → access token mới + cookie mới (xoay vòng).
        var res = await LamMoiAsync(refresh);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var moi = (await res.Content.ReadFromJsonAsync<TokenResponse>(Ct))!;
        var refreshMoi = UpmApiFactory.LayRefreshCookie(res);
        Assert.NotNull(refreshMoi);
        Assert.NotEqual(refresh, refreshMoi);

        // 3. Token mới dùng được.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", moi.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me", Ct)).StatusCode);
    }

    [Fact]
    public async Task Dung_lai_refresh_token_cu_sau_an_han_thi_thu_hoi_ca_ho_token()
    {
        var (_, _, refresh1) = await factory.DangNhapAsync("gv004", Ct);
        var refresh2 = UpmApiFactory.LayRefreshCookie(await LamMoiAsync(refresh1));
        Assert.NotNull(refresh2);

        try
        {
            factory.Clock.Lech = TimeSpan.FromSeconds(31); // quá 30 giây ân hạn nhiều tab

            // Kẻ gian dùng lại token cũ → 401 và cả họ bị thu hồi…
            var dungLai = await LamMoiAsync(refresh1);
            Assert.Equal(HttpStatusCode.Unauthorized, dungLai.StatusCode);
            // …nên token hợp lệ mới nhất cũng không dùng được nữa.
            Assert.Equal(HttpStatusCode.Unauthorized, (await LamMoiAsync(refresh2!)).StatusCode);
        }
        finally
        {
            factory.Clock.Lech = TimeSpan.Zero;
        }
    }

    [Fact]
    public async Task Nhieu_tab_refresh_cung_luc_trong_an_han_khong_bi_dang_xuat()
    {
        var (_, _, refresh1) = await factory.DangNhapAsync("gv003", Ct);

        var tab1 = await LamMoiAsync(refresh1);
        var tab2 = await LamMoiAsync(refresh1); // tab khác gửi token cũ ngay sau đó

        Assert.Equal(HttpStatusCode.OK, tab1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tab2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LamMoiAsync(UpmApiFactory.LayRefreshCookie(tab1)!)).StatusCode);
    }

    [Fact]
    public async Task Dang_xuat_thu_hoi_refresh_token_va_xoa_cookie()
    {
        var (_, _, refresh) = await factory.DangNhapAsync("thuvien.hn", Ct);

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        req.Headers.Add("Cookie", $"upm_rt={refresh}");
        var res = await factory.CreateRawClient().SendAsync(req, Ct);

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        Assert.Null(UpmApiFactory.LayRefreshCookie(res)); // cookie bị xóa (giá trị rỗng)
        Assert.Equal(HttpStatusCode.Unauthorized, (await LamMoiAsync(refresh)).StatusCode);
    }

    [Fact]
    public async Task Refresh_khong_co_cookie_tra_401()
    {
        var res = await factory.CreateRawClient().PostAsync("/api/v1/auth/refresh", null, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    private async Task<HttpResponseMessage> LamMoiAsync(string refreshToken)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        req.Headers.Add("Cookie", $"upm_rt={refreshToken}");
        return await factory.CreateRawClient().SendAsync(req, Ct);
    }

    private static string TaoTokenHetHan(long taiKhoanId)
    {
        var now = DateTime.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "upm-api",
            Audience = "upm-web",
            Subject = new ClaimsIdentity([new Claim("sub", taiKhoanId.ToString(System.Globalization.CultureInfo.InvariantCulture))]),
            IssuedAt = now.AddMinutes(-20),
            NotBefore = now.AddMinutes(-20),
            Expires = now.AddMinutes(-5),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(UpmApiFactory.JwtSigningKey)), SecurityAlgorithms.HmacSha256),
        });
    }
}
