using Microsoft.EntityFrameworkCore;
using Upm.Application.Abstractions;
using Upm.Application.PhanQuyen;
using Upm.Domain.Enums;
using Upm.Infrastructure.Persistence;

namespace Upm.Infrastructure.Identity;

/// <summary>
/// Cài đặt tạm dựa trên PhanCong / LichBaoVe + ThanhVienHoiDong / SVTrongDot.
/// Phân công "còn hiệu lực" = NgayKetThuc NULL hoặc chưa tới (đổi GVHD đóng dòng cũ – B08).
/// </summary>
public sealed class QuyenTrenSV(UpmDbContext db, ICurrentUser currentUser, IClock clock) : IQuyenTrenSV
{
    public Task<bool> LaGVHDAsync(long svTrongDotId, CancellationToken ct = default) =>
        CoPhanCongAsync(svTrongDotId, VaiTroPhanCong.GVHD, ct);

    public Task<bool> LaGVDDAsync(long svTrongDotId, CancellationToken ct = default) =>
        CoPhanCongAsync(svTrongDotId, VaiTroPhanCong.GVDD, ct);

    public async Task<bool> LaThanhVienHoiDongAsync(long svTrongDotId, CancellationToken ct = default)
    {
        if (currentUser.GiangVienId is not { } gv)
        {
            return false;
        }

        return await (from l in db.LichBaoVe
                      join tv in db.ThanhVienHoiDong on l.HoiDongId equals tv.HoiDongId
                      where l.SVTrongDotId == svTrongDotId && tv.GiangVienId == gv
                      select tv.Id).AnyAsync(ct);
    }

    public async Task<bool> LaChinhSVAsync(long svTrongDotId, CancellationToken ct = default)
    {
        if (currentUser.SinhVienId is not { } sv)
        {
            return false;
        }

        return await db.SVTrongDot.AnyAsync(x => x.Id == svTrongDotId && x.SinhVienId == sv, ct);
    }

    private async Task<bool> CoPhanCongAsync(long svTrongDotId, VaiTroPhanCong vaiTro, CancellationToken ct)
    {
        if (currentUser.GiangVienId is not { } gv)
        {
            return false;
        }

        var homNay = clock.Today;
        return await db.PhanCong.AnyAsync(
            p => p.SVTrongDotId == svTrongDotId && p.GiangVienId == gv && p.VaiTro == vaiTro
                 && (p.NgayKetThuc == null || p.NgayKetThuc >= homNay),
            ct);
    }
}
