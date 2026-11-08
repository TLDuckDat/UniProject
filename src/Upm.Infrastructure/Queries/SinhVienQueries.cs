using Microsoft.EntityFrameworkCore;
using Upm.Application.Common;
using Upm.Application.PhanQuyen;
using Upm.Application.TraCuuSinhVien;
using Upm.Infrastructure.Persistence;

namespace Upm.Infrastructure.Queries;

public sealed class SinhVienQueries(UpmDbContext db, IScopeFilter scope) : ISinhVienQueries
{
    public async Task<PagedResult<SinhVienListItem>> DanhSachAsync(string? tuKhoa, PageRequest page, CancellationToken ct = default)
    {
        var q = await scope.LocAsync(db.SinhVien.AsNoTracking(), ct: ct);

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var k = tuKhoa.Trim();
            q = q.Where(s => s.MaSV.Contains(k) || s.HoTen.Contains(k));
        }

        var total = await q.CountAsync(ct);
        var items = await (from s in q
                           join c in db.ChuongTrinhDaoTao on s.CTDTId equals c.Id
                           join k in db.Khoa on c.KhoaId equals k.Id
                           join cs in db.CoSo on s.CoSoId equals cs.Id
                           orderby s.MaSV
                           select new SinhVienListItem(
                               s.Id, s.MaSV, s.HoTen, s.KhoaHoc, cs.MaCoSo, k.TenKhoa, c.MaCTDT, s.TrangThaiHocTap.ToString()))
            .Skip(page.Skip)
            .Take(page.SafePageSize)
            .ToListAsync(ct);

        return new PagedResult<SinhVienListItem>(items, page.SafePage, page.SafePageSize, total);
    }
}
