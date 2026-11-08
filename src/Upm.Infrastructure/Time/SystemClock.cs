using Microsoft.Extensions.Configuration;
using Upm.Application.Abstractions;

namespace Upm.Infrastructure.Time;

/// <summary>Đồng hồ hệ thống theo múi giờ nghiệp vụ (cấu hình <c>Clock:TimeZone</c>, mặc định Asia/Ho_Chi_Minh).</summary>
public sealed class SystemClock : IClock
{
    public const string DefaultTimeZone = "Asia/Ho_Chi_Minh";

    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;

    public SystemClock(TimeProvider timeProvider, IConfiguration configuration)
    {
        _timeProvider = timeProvider;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(configuration["Clock:TimeZone"] ?? DefaultTimeZone);
    }

    public DateTimeOffset Now => TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), _timeZone);

    public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);
}
