using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;
using Upm.Application.Abstractions;
using Upm.Application.XacThuc;
using Upm.Infrastructure.Persistence.Seed;

namespace Upm.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Khởi động API thật trên SQL Server 2022 chạy trong container (Testcontainers).
/// Cần Docker đang chạy. Dùng chung cho mọi lớp test qua <see cref="SharedApiFixture"/>.
/// </summary>
public sealed class UpmApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-CU17-ubuntu-22.04";
    public const string JwtSigningKey = "upm-integration-test-signing-key-0123456789-abcdef";

    private readonly MsSqlContainer _sql = new MsSqlBuilder(SqlServerImage).Build();
    private readonly SemaphoreSlim _seedLock = new(1, 1);
    private bool _daSeed;

    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>Đồng hồ dùng chung của API trong test – chỉnh <see cref="TestClock.Lech"/> để "tua" thời gian.</summary>
    public TestClock Clock { get; } = new();

    /// <summary>Email gửi đi được ghi lại trong bộ nhớ (không gửi thật).</summary>
    public RecordingEmailSender Email { get; } = new();

    /// <summary>Thư mục tạm cho file tải lên trong test.</summary>
    public string TepThuMuc { get; } = Path.Combine(Path.GetTempPath(), "upm-test-tep-" + Guid.NewGuid().ToString("N"));

    public async ValueTask InitializeAsync()
    {
        await _sql.StartAsync();
        ConnectionString = new SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            InitialCatalog = "UpmTest",
            TrustServerCertificate = true,
        }.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // UseSetting được áp dụng trước khi Program.cs đọc cấu hình.
        builder.UseSetting("ConnectionStrings:Upm", ConnectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Swagger:Enabled", "true");
        builder.UseSetting("Jwt:SigningKey", JwtSigningKey);
        builder.UseSetting("Jwt:RefreshCookieSecure", "false"); // TestServer chạy HTTP
        // Giữ nguyên giới hạn đăng nhập thật (300/IP/phút, 5 lần sai/15 phút) để test đúng cấu hình.
        builder.UseSetting("Hangfire:DashboardEnabled", "true");

        builder.UseSetting("Files:Root", TepThuMuc);
        // Test kiểm soát đạo văn bằng nhập tay; không để giả lập tự quét (kết quả phụ thuộc nội dung file).
        builder.UseSetting("Quyen:DaoVanTuDong", "false");
        builder.ConfigureTestServices(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IClock>(Clock));
            services.Replace(ServiceDescriptor.Singleton<Upm.Application.TienDo.IEmailSender>(Email));
        });
    }

    /// <summary>Scope DI mới để lấy UpmDbContext / service thật của API.</summary>
    public AsyncServiceScope CreateScope() => Services.CreateAsyncScope();

    /// <summary>Connection string tới một database KHÁC trên cùng container (test cần schema riêng).</summary>
    public string ConnectionStringFor(string database) =>
        new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = database }.ConnectionString;

    /// <summary>Nạp dữ liệu mẫu + tài khoản dev (một lần, idempotent).</summary>
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await _seedLock.WaitAsync(ct);
        try
        {
            if (_daSeed)
            {
                return;
            }
            await using var scope = CreateScope();
            await scope.ServiceProvider.GetRequiredService<DevDataSeeder>().SeedAsync(ct);
            _daSeed = true;
        }
        finally
        {
            _seedLock.Release();
        }
    }

    /// <summary>Tạo tài khoản (vai trò GiangVien, không phạm vi) với mật khẩu cho trước – dùng trong test đăng nhập.</summary>
    public async Task<long> TaoTaiKhoanAsync(string tenDangNhap, string matKhau, CancellationToken ct = default)
    {
        await SeedAsync(ct);
        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Upm.Infrastructure.Persistence.UpmDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<Upm.Domain.Entities.TaiKhoan>>();
        var tk = new Upm.Domain.Entities.TaiKhoan { TenDangNhap = tenDangNhap, LoaiTK = Upm.Domain.Enums.LoaiTaiKhoan.NoiBo };
        tk.MatKhauHash = hasher.HashPassword(tk, matKhau);
        db.TaiKhoan.Add(tk);
        await db.SaveChangesAsync(ct);
        return tk.Id;
    }

    /// <summary>Client KHÔNG tự quản lý cookie (test tự gửi cookie refresh khi cần).</summary>
    public HttpClient CreateRawClient() => CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    /// <summary>Đăng nhập tài khoản dev, trả client đã gắn Bearer + refresh token (từ Set-Cookie).</summary>
    public async Task<(HttpClient Client, TokenResponse Token, string RefreshToken)> DangNhapAsync(
        string tenDangNhap, CancellationToken ct = default)
    {
        await SeedAsync(ct);
        var client = CreateRawClient();
        var res = await client.PostAsJsonAsync("/api/v1/auth/login", new DangNhapRequest(tenDangNhap, DevDataSeeder.MatKhauDev), ct);
        res.EnsureSuccessStatusCode();
        var token = (await res.Content.ReadFromJsonAsync<TokenResponse>(ct))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return (client, token, LayRefreshCookie(res)!);
    }

    /// <summary>Đọc giá trị cookie upm_rt từ Set-Cookie (null nếu không có / bị xóa).</summary>
    public static string? LayRefreshCookie(HttpResponseMessage res)
    {
        if (!res.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }
        var dong = cookies.FirstOrDefault(c => c.StartsWith("upm_rt=", StringComparison.Ordinal));
        var giaTri = dong?["upm_rt=".Length..].Split(';')[0];
        return string.IsNullOrEmpty(giaTri) ? null : giaTri;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
        _seedLock.Dispose();
    }
}

/// <summary>IEmailSender trong test: lưu lại thư gửi đi để kiểm tra "không gửi trùng".</summary>
public sealed class RecordingEmailSender : Upm.Application.TienDo.IEmailSender
{
    private readonly List<(string To, string Subject, string Body)> _thu = [];
    private readonly object _khoa = new();

    public Task GuiAsync(string denEmail, string tieuDe, string noiDung, CancellationToken ct = default)
    {
        lock (_khoa) _thu.Add((denEmail, tieuDe, noiDung));
        return Task.CompletedTask;
    }

    public IReadOnlyList<(string To, string Subject, string Body)> DaGui
    {
        get { lock (_khoa) return _thu.ToArray(); }
    }
}

/// <summary>IClock cho test: giờ thật (múi giờ VN) cộng thêm độ lệch điều chỉnh được.</summary>
public sealed class TestClock : IClock
{
    private static readonly TimeZoneInfo Vn = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    public TimeSpan Lech { get; set; }

    public DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow + Lech, Vn);

    public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);
}
