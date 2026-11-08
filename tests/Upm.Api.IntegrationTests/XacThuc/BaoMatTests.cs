using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Upm.Api.IntegrationTests.Infrastructure;
using Upm.Application.XacThuc;

namespace Upm.Api.IntegrationTests.XacThuc;

/// <summary>Phần 3b – C5 giới hạn đăng nhập, C6 CSRF theo Origin, C8 dashboard Hangfire.</summary>
[Collection(SharedApiFixture.Name)]
public sealed class BaoMatTests(UpmApiFactory factory)
{
    private const string MatKhau = "Nat@123456";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ---------- C5: giới hạn đăng nhập theo (tài khoản, IP) ----------

    [Fact]
    public async Task Hai_muoi_tai_khoan_khac_nhau_cung_IP_dang_nhap_lien_tiep_khong_bi_chan()
    {
        for (var i = 1; i <= 20; i++)
        {
            await factory.TaoTaiKhoanAsync($"nat.sv{i:00}", MatKhau, Ct);
        }

        var client = factory.CreateRawClient(); // cùng một "IP" của TestServer
        for (var i = 1; i <= 20; i++)
        {
            var res = await client.PostAsJsonAsync("/api/v1/auth/login", new DangNhapRequest($"nat.sv{i:00}", MatKhau), Ct);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }
    }

    [Fact]
    public async Task Mot_tai_khoan_sai_mat_khau_lien_tuc_bi_khoa_tam_tai_khoan_khac_van_vao_duoc()
    {
        await factory.TaoTaiKhoanAsync("do.mk.nannhan", MatKhau, Ct);
        await factory.TaoTaiKhoanAsync("do.mk.hangxom", MatKhau, Ct);
        var client = factory.CreateRawClient();

        for (var i = 0; i < 5; i++)
        {
            var sai = await client.PostAsJsonAsync("/api/v1/auth/login", new DangNhapRequest("do.mk.nannhan", "sai"), Ct);
            Assert.Equal(HttpStatusCode.Unauthorized, sai.StatusCode);
        }

        // Lần 6 – kể cả đúng mật khẩu – bị chặn 429 + Retry-After.
        var biKhoa = await client.PostAsJsonAsync("/api/v1/auth/login", new DangNhapRequest("do.mk.nannhan", MatKhau), Ct);
        Assert.Equal(HttpStatusCode.TooManyRequests, biKhoa.StatusCode);
        Assert.True(biKhoa.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        using (var json = JsonDocument.Parse(await biKhoa.Content.ReadAsStringAsync(Ct)))
        {
            Assert.Equal("UPM_AUTH_TAM_KHOA", json.RootElement.GetProperty("code").GetString());
        }

        // Tài khoản khác cùng IP không bị ảnh hưởng.
        var hangXom = await client.PostAsJsonAsync("/api/v1/auth/login", new DangNhapRequest("do.mk.hangxom", MatKhau), Ct);
        Assert.Equal(HttpStatusCode.OK, hangXom.StatusCode);

        // Hết 15 phút → đăng nhập lại được.
        try
        {
            factory.Clock.Lech = TimeSpan.FromMinutes(16);
            var sauKhoa = await client.PostAsJsonAsync("/api/v1/auth/login", new DangNhapRequest("do.mk.nannhan", MatKhau), Ct);
            Assert.Equal(HttpStatusCode.OK, sauKhoa.StatusCode);
        }
        finally
        {
            factory.Clock.Lech = TimeSpan.Zero;
        }
    }

    [Fact]
    public async Task Dang_nhap_dung_thi_xoa_bo_dem_lan_sai()
    {
        await factory.TaoTaiKhoanAsync("quen.mk", MatKhau, Ct);
        var client = factory.CreateRawClient();

        for (var vong = 0; vong < 2; vong++)
        {
            for (var i = 0; i < 4; i++)
            {
                await client.PostAsJsonAsync("/api/v1/auth/login", new DangNhapRequest("quen.mk", "sai"), Ct);
            }
            var dung = await client.PostAsJsonAsync("/api/v1/auth/login", new DangNhapRequest("quen.mk", MatKhau), Ct);
            Assert.Equal(HttpStatusCode.OK, dung.StatusCode); // 4 + 4 lần sai nhưng xen lần đúng → không khóa
        }
    }

    // ---------- C6: CSRF – refresh/logout kiểm Origin ----------

    [Theory]
    [InlineData("http://evil.example.com", HttpStatusCode.Forbidden)]
    [InlineData("null", HttpStatusCode.Forbidden)]
    [InlineData("http://localhost:4200", HttpStatusCode.OK)]
    public async Task Refresh_kiem_tra_Origin(string origin, HttpStatusCode mongDoi)
    {
        var (_, _, refresh) = await factory.DangNhapAsync("ldk.ct.hn", Ct);

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        req.Headers.Add("Cookie", $"upm_rt={refresh}");
        req.Headers.Add("Origin", origin);
        var res = await factory.CreateRawClient().SendAsync(req, Ct);

        Assert.Equal(mongDoi, res.StatusCode);
        if (mongDoi == HttpStatusCode.Forbidden)
        {
            using var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync(Ct));
            Assert.Equal("UPM_CSRF_ORIGIN", json.RootElement.GetProperty("code").GetString());
        }
    }

    [Fact]
    public async Task Logout_tu_Origin_la_bi_403_va_khong_thu_hoi_token()
    {
        var (_, _, refresh) = await factory.DangNhapAsync("giaovu.ct.hn", Ct);

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        req.Headers.Add("Cookie", $"upm_rt={refresh}");
        req.Headers.Add("Origin", "https://attacker.example");
        Assert.Equal(HttpStatusCode.Forbidden, (await factory.CreateRawClient().SendAsync(req, Ct)).StatusCode);

        // Token vẫn còn dùng được.
        var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq.Headers.Add("Cookie", $"upm_rt={refresh}");
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateRawClient().SendAsync(refreshReq, Ct)).StatusCode);
    }

    // ---------- C8: dashboard Hangfire chỉ QuanTri ----------

    [Fact]
    public async Task Hangfire_dashboard_chua_dang_nhap_bi_tu_choi()
    {
        var res = await factory.CreateRawClient().GetAsync("/hangfire", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Hangfire_dashboard_vai_tro_khac_QuanTri_bi_tu_choi()
    {
        var (_, token, _) = await factory.DangNhapAsync("pdt.hn", Ct);

        var res = await factory.CreateRawClient().GetAsync($"/hangfire?access_token={token.AccessToken}", Ct);

        Assert.True(res.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized, res.StatusCode.ToString());
    }

    [Fact]
    public async Task Hangfire_dashboard_QuanTri_vao_duoc_va_cac_request_sau_dung_cookie()
    {
        var (_, token, _) = await factory.DangNhapAsync("admin", Ct);
        var client = factory.CreateRawClient();

        var res = await client.GetAsync($"/hangfire?access_token={token.AccessToken}", Ct);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var cookie = Assert.Single(res.Headers.GetValues("Set-Cookie"), c => c.StartsWith("upm_hf=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/hangfire", cookie, StringComparison.OrdinalIgnoreCase);

        var sau = new HttpRequestMessage(HttpMethod.Get, "/hangfire/jobs/enqueued");
        sau.Headers.Add("Cookie", cookie.Split(';')[0]);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(sau, Ct)).StatusCode);
    }
}
