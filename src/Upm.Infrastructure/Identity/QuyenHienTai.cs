using Microsoft.EntityFrameworkCore;
using Upm.Application.Abstractions;
using Upm.Application.PhanQuyen;
using Upm.Domain.PhanQuyen;
using Upm.Infrastructure.Persistence;

namespace Upm.Infrastructure.Identity;

/// <summary>
/// Nạp vai trò + phạm vi từ TaiKhoanVaiTro mỗi request (thu hồi quyền có hiệu lực ngay, không chờ token hết hạn).
/// Cache trong scope. CoSoId của dòng phạm vi khoa/bộ môn được suy ra qua Khoa/BoMon.
/// </summary>
public sealed class QuyenHienTai(UpmDbContext db, ICurrentUser currentUser) : IQuyenHienTai
{
    private QuyenNguoiDung? _cache;

    public async Task<QuyenNguoiDung> LayAsync(CancellationToken ct = default)
    {
        if (_cache is not null)
        {
            return _cache;
        }

        if (currentUser.TaiKhoanId is not { } taiKhoanId)
        {
            return _cache = QuyenNguoiDung.KhongCo;
        }

        var vaiTro = await NapPhamViAsync(db, taiKhoanId, ct);

        IReadOnlyList<long> coSoDuocPhep = vaiTro.Any(v => v.ToanHeThong)
            ? await db.CoSo.Where(c => c.ConHoatDong).Select(c => c.Id).ToListAsync(ct)
            : vaiTro.Where(v => v.CoSoId is not null).Select(v => v.CoSoId!.Value).Distinct().ToList();

        // Cơ sở client chọn chỉ có hiệu lực khi nằm trong danh sách được phép; sai → bỏ qua (không mở rộng quyền).
        var coSoChon = currentUser.CoSoDangChonId is { } chon && coSoDuocPhep.Contains(chon) ? chon : (long?)null;

        return _cache = new QuyenNguoiDung(
            vaiTro,
            BangQuyen.ChoVaiTro(vaiTro.Select(v => v.MaVaiTro)),
            coSoDuocPhep,
            coSoChon);
    }

    internal static async Task<List<PhamViVaiTro>> NapPhamViAsync(UpmDbContext db, long taiKhoanId, CancellationToken ct)
    {
        var rows = await (from tv in db.TaiKhoanVaiTro
                          join vt in db.VaiTro on tv.VaiTroId equals vt.Id
                          join tk in db.TaiKhoan on tv.TaiKhoanId equals tk.Id
                          where tv.TaiKhoanId == taiKhoanId && tk.ConHoatDong
                          select new
                          {
                              vt.MaVaiTro,
                              tv.CoSoId,
                              tv.KhoaId,
                              tv.BoMonId,
                              KhoaCuaBoMon = db.BoMon.Where(b => b.Id == tv.BoMonId).Select(b => (long?)b.KhoaId).FirstOrDefault(),
                          }).ToListAsync(ct);

        var khoaIds = rows.Select(r => r.KhoaId ?? r.KhoaCuaBoMon).Where(k => k is not null).Select(k => k!.Value).Distinct().ToList();
        var coSoCuaKhoa = await db.Khoa.Where(k => khoaIds.Contains(k.Id)).ToDictionaryAsync(k => k.Id, k => k.CoSoId, ct);

        return rows.Select(r =>
        {
            var khoa = r.KhoaId ?? r.KhoaCuaBoMon;
            var coSo = r.CoSoId ?? (khoa is { } k && coSoCuaKhoa.TryGetValue(k, out var cs) ? cs : null);
            return new PhamViVaiTro(r.MaVaiTro, coSo, khoa, r.BoMonId);
        }).ToList();
    }
}
