using Microsoft.EntityFrameworkCore;

namespace Upm.Infrastructure.Persistence;

/// <summary>
/// DbContext chính. Phần 1 chưa có entity nghiệp vụ; cấu hình bảng (IEntityTypeConfiguration)
/// đặt trong <c>Persistence/Configurations</c> và được nạp tự động.
/// </summary>
public class UpmDbContext(DbContextOptions<UpmDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UpmDbContext).Assembly);
    }
}
