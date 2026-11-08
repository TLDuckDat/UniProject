using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Upm.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Chạy migration EF khi cấu hình <c>Database:MigrateOnStartup = true</c> (dev/test).
    /// Phải chạy TRƯỚC khi Hangfire khởi động vì Hangfire cần database đã tồn tại để tạo schema.
    /// </summary>
    public static async Task MigrateDatabaseIfConfiguredAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();

        logger.LogInformation("Đang áp dụng migration cho cơ sở dữ liệu...");
        await db.Database.MigrateAsync(ct);
        logger.LogInformation("Đã áp dụng migration.");

        // Dữ liệu mẫu chỉ bật ở Development (appsettings.Development.json).
        if (configuration.GetValue<bool>("Database:SeedSampleData"))
        {
            await scope.ServiceProvider.GetRequiredService<Seed.DevDataSeeder>().SeedAsync(ct);
        }
    }
}
