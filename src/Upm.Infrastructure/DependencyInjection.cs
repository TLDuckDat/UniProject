using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Upm.Application.Abstractions;
using Upm.Application.PhanQuyen;
using Upm.Application.TraCuuSinhVien;
using Upm.Application.XacThuc;
using Upm.Domain.Entities;
using Upm.Infrastructure.Identity;
using Upm.Infrastructure.Queries;
using Upm.Infrastructure.Persistence;
using Upm.Infrastructure.Persistence.Interceptors;
using Upm.Infrastructure.Persistence.Seed;
using Upm.Infrastructure.Time;

namespace Upm.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Upm";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, SystemClock>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<IAuditContext, AuditContext>();
        services.AddScoped<AuditFieldsInterceptor>();
        services.AddScoped<AuditLogInterceptor>();
        services.AddScoped<DevDataSeeder>();

        // Xác thực & phân quyền
        services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.SectionName)
            .Validate(o => System.Text.Encoding.UTF8.GetByteCount(o.SigningKey) >= 32, "Jwt:SigningKey phải dài tối thiểu 32 byte.")
            .ValidateOnStart();
        services.AddOptions<XacThucOptions>().BindConfiguration(XacThucOptions.SectionName);
        services.AddMemoryCache();
        services.AddSingleton<DangNhapSaiTracker>();
        services.AddSingleton<IPasswordHasher<TaiKhoan>, PasswordHasher<TaiKhoan>>();
        services.AddScoped<TokenService>();
        services.AddScoped<IXacThucService, XacThucService>();
        services.AddScoped<IQuyenHienTai, QuyenHienTai>();
        services.AddScoped<IScopeFilter, ScopeFilter>();
        services.AddScoped<IQuyenTrenSV, QuyenTrenSV>();

        // Truy vấn
        services.AddScoped<ISinhVienQueries, SinhVienQueries>();

        // Đọc connection string lúc resolve (không đọc lúc đăng ký) để integration test ghi đè được cấu hình.
        // Thứ tự interceptor: điền NgayTao/NgaySua trước, rồi mới chụp giá trị cho AuditLog.
        services.AddDbContext<UpmDbContext>((sp, options) => options
            .UseSqlServer(GetConnectionString(sp))
            .AddInterceptors(
                sp.GetRequiredService<AuditFieldsInterceptor>(),
                sp.GetRequiredService<AuditLogInterceptor>()));

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
