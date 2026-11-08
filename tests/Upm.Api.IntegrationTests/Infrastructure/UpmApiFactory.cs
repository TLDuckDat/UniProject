using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Upm.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Khởi động API thật trên SQL Server 2022 chạy trong container (Testcontainers).
/// Cần Docker đang chạy. Một container dùng chung cho cả lớp test (IClassFixture).
/// </summary>
public sealed class UpmApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-latest";

    private readonly MsSqlContainer _sql = new MsSqlBuilder(SqlServerImage).Build();

    public string ConnectionString { get; private set; } = string.Empty;

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
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
    }
}
