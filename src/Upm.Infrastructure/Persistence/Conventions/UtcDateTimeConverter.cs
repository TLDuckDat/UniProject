using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Upm.Infrastructure.Persistence.Conventions;

/// <summary>
/// Mọi cột datetime2 lưu UTC. Ghi: Local → UTC, Unspecified coi là UTC. Đọc: gắn Kind = Utc.
/// </summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => ToUtc(v),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
{
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
