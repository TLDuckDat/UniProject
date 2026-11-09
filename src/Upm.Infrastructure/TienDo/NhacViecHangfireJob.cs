using Upm.Application.Abstractions;
using Upm.Application.TienDo;

namespace Upm.Infrastructure.TienDo;

/// <summary>
/// Vỏ Hangfire cho <see cref="INhacViecService"/>. Đăng ký lịch hằng ngày ở API (xem Program.cs); không tự retry
/// để tránh gửi lại: chống trùng đã nằm trong dữ liệu (UK <c>LichNhacViec</c>).
/// </summary>
public sealed class NhacViecHangfireJob(INhacViecService service, IClock clock)
{
    public const string IdLich = "upm-nhac-viec-tien-do-hang-ngay";

    public Task ChayAsync(CancellationToken ct) => service.ChayAsync(DateOnly.FromDateTime(clock.Now.DateTime), ct);
}
