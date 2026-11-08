using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Upm.Application.Abstractions;

namespace Upm.Infrastructure.Identity;

/// <summary>
/// Đếm lần đăng nhập sai theo cặp (TenDangNhap, IP). Đủ <see cref="DangNhapSaiOptions.SoLanToiDa"/> lần trong
/// <see cref="DangNhapSaiOptions.ThoiGianKhoaPhut"/> phút → khóa tạm cặp đó (tài khoản khác cùng IP vẫn đăng nhập được;
/// cùng tài khoản từ IP khác cũng không bị ảnh hưởng – tránh kẻ xấu cố tình khóa tài khoản người khác).
/// Lưu trong bộ nhớ (một instance API) – xem Nợ kỹ thuật khi chạy nhiều instance.
/// </summary>
public sealed class DangNhapSaiTracker(IMemoryCache cache, IOptions<XacThucOptions> options, IClock clock)
{
    private readonly object _lock = new();
    private DangNhapSaiOptions Cfg => options.Value.DangNhapSai;

    private sealed class Dem
    {
        public int SoLan;
        public DateTimeOffset HetHan;
    }

    /// <summary>Thời gian còn bị khóa, null nếu không bị khóa.</summary>
    public TimeSpan? ConBiKhoa(string tenDangNhap, string? ip)
    {
        var now = clock.Now;
        return cache.TryGetValue(Key(tenDangNhap, ip), out Dem? d) && d!.SoLan >= Cfg.SoLanToiDa && d.HetHan > now
            ? d.HetHan - now
            : null;
    }

    /// <summary>Ghi nhận một lần sai; trả về true nếu lần này làm cặp (tài khoản, IP) bị khóa.</summary>
    public bool GhiNhanSai(string tenDangNhap, string? ip)
    {
        var key = Key(tenDangNhap, ip);
        var now = clock.Now;
        var cuaSo = TimeSpan.FromMinutes(Cfg.ThoiGianKhoaPhut);
        lock (_lock)
        {
            if (!cache.TryGetValue(key, out Dem? d) || d!.HetHan <= now)
            {
                d = new Dem { SoLan = 0, HetHan = now + cuaSo };
            }

            d.SoLan++;
            var biKhoa = d.SoLan >= Cfg.SoLanToiDa;
            if (biKhoa)
            {
                d.HetHan = now + cuaSo; // khóa đủ thời gian tính từ lần sai cuối
            }

            cache.Set(key, d, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = cuaSo + TimeSpan.FromMinutes(1) });
            return biKhoa;
        }
    }

    public void XoaDem(string tenDangNhap, string? ip) => cache.Remove(Key(tenDangNhap, ip));

    private static string Key(string tenDangNhap, string? ip) =>
        $"dang-nhap-sai:{tenDangNhap.Trim().ToLowerInvariant()}:{ip ?? "?"}";
}
