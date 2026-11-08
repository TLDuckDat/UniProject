using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Upm.Api.IntegrationTests.Infrastructure;
using Upm.Application.SystemInfo;

namespace Upm.Api.IntegrationTests;

public sealed class SystemEndpointsTests(UpmApiFactory factory) : IClassFixture<UpmApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Ping_tra_ve_pong_version_va_thoi_gian_may_chu()
    {
        var before = DateTimeOffset.UtcNow.AddMinutes(-1);

        var response = await _client.GetAsync("/api/v1/ping", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PingResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("pong", body.Message);
        Assert.Matches(@"^\d+\.\d+\.\d+", body.Version);
        Assert.InRange(body.ServerTime, before, DateTimeOffset.UtcNow.AddMinutes(1));
        // IClock trả giờ Việt Nam (UTC+7).
        Assert.Equal(TimeSpan.FromHours(7), body.ServerTime.Offset);
    }

    [Fact]
    public async Task Health_tra_ve_Healthy_khi_ket_noi_duoc_database()
    {
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Route_khong_ton_tai_tra_ProblemDetails_tieng_Viet_co_code()
    {
        var response = await _client.GetAsync("/api/v1/khong-ton-tai", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("UPM_NOT_FOUND", json.RootElement.GetProperty("code").GetString());
        Assert.Equal("Không tìm thấy tài nguyên yêu cầu.", json.RootElement.GetProperty("title").GetString());
        Assert.True(json.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Swagger_json_mo_ta_endpoint_ping()
    {
        var json = await _client.GetStringAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Contains("/api/v1/ping", json, StringComparison.Ordinal);
    }
}
