using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Upm.Application.Abstractions;
using Upm.Infrastructure.Persistence;
using Upm.Infrastructure.Time;

namespace Upm.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Upm";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, SystemClock>();

        // Đọc connection string lúc resolve (không đọc lúc đăng ký) để integration test ghi đè được cấu hình.
        services.AddDbContext<UpmDbContext>((sp, options) =>
            options.UseSqlServer(GetConnectionString(sp)));

        services.AddHealthChecks()
            .AddDbContextCheck<UpmDbContext>("database", tags: ["db"]);

        // Hangfire: storage SQL Server (schema HangFire trong cùng database). Chưa có job ở Phần 1.
        services.AddHangfire((sp, config) => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(
                () => new SqlConnection(GetConnectionString(sp)),
                new SqlServerStorageOptions
                {
                    SchemaName = "HangFire",
                    PrepareSchemaIfNecessary = true,
                }));
        services.AddHangfireServer(options => options.ServerName = $"upm-{Environment.MachineName}");

        return services;
    }

    private static string GetConnectionString(IServiceProvider sp) =>
        sp.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
        ?? throw new InvalidOperationException($"Thiếu connection string \"{ConnectionStringName}\".");
}
