using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Upm.Domain.Common;

namespace Upm.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    /// <summary>Tên bảng + khóa chính Id (bigint IDENTITY).</summary>
    public static EntityTypeBuilder<T> ToUpmTable<T>(this EntityTypeBuilder<T> builder, string table)
        where T : Entity
    {
        builder.ToTable(table);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        return builder;
    }

    /// <summary>
    /// FK qua navigation tham chiếu (many-to-one), LUÔN <see cref="DeleteBehavior.Restrict"/> (không xóa dây chuyền).
    /// Bắt buộc/tùy chọn suy ra từ kiểu (long / long?). <paramref name="collection"/>: navigation ngược phía principal
    /// (chỉ khai báo khi thật cần). EF tự tạo index cho cột FK.
    /// </summary>
    public static EntityTypeBuilder<T> HasFk<T, TPrincipal>(
        this EntityTypeBuilder<T> builder,
        Expression<Func<T, TPrincipal?>> navigation,
        Expression<Func<T, object?>> foreignKey,
        Expression<Func<TPrincipal, IEnumerable<T>?>>? collection = null)
        where T : Entity
        where TPrincipal : Entity
    {
        builder.HasOne(navigation)
            .WithMany(collection)
            .HasForeignKey(foreignKey)
            .OnDelete(DeleteBehavior.Restrict);
        return builder;
    }

    /// <summary>nvarchar(n) (string hoặc string?).</summary>
    public static PropertyBuilder<T> Str<T>(this PropertyBuilder<T> property, int maxLength) =>
        property.HasMaxLength(maxLength);

    /// <summary>nvarchar(max).</summary>
    public static PropertyBuilder<T> StrMax<T>(this PropertyBuilder<T> property) =>
        property.HasColumnType("nvarchar(max)");
}
