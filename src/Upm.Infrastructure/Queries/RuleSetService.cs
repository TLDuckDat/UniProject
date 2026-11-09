using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Upm.Application.Common;
using Upm.Application.PhanQuyen;
using Upm.Application.RuleSets;
using Upm.Domain.Common;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Domain.Rules;
using Upm.Infrastructure.Persistence;

namespace Upm.Infrastructure.Queries;

public sealed class RuleSetService(UpmDbContext db, IScopeFilter scope) : IRuleSetService
{
    public async Task<PagedResult<RuleSetItem>> DanhSachAsync(string? q, PageRequest page, CancellationToken ct = default)
    {
        var query = db.RuleSet.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.MaRuleSet.Contains(q) || x.TenRuleSet.Contains(q));
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.HieuLucTu).ThenBy(x => x.MaRuleSet).Skip(page.Skip).Take(page.SafePageSize).ToListAsync(ct);
        return new PagedResult<RuleSetItem>(await Map(rows, ct), page.SafePage, page.SafePageSize, total);
    }

    public async Task<RuleSetItem> LayAsync(long id, CancellationToken ct = default) =>
        (await Map([await TimRule(id, ct)], ct)).Single();

    public async Task<RuleSetItem> TaoAsync(RuleSetRequest request, CancellationToken ct = default)
    {
        var r = request;
        KiemTra(r);
        var x = Tao(r);
        db.RuleSet.Add(x);
        await db.SaveChangesAsync(ct);
        return await LayAsync(x.Id, ct);
    }

    public async Task<RuleSetItem> SuaAsync(long id, RuleSetRequest request, CancellationToken ct = default)
    {
        var r = request;
        var x = await TimRule(id, ct);
        await KiemTraChuaDung(x.Id, ct);
        KiemTra(r);
        Salin(x, r);
        await db.SaveChangesAsync(ct);
        return await LayAsync(id, ct);
    }

    public async Task<RuleSetItem> NhanBanAsync(long id, string maMoi, string tenMoi, CancellationToken ct = default)
    {
        var source = await TimRule(id, ct);
        if (string.IsNullOrWhiteSpace(maMoi) || string.IsNullOrWhiteSpace(tenMoi)) throw new DomainException("UPM_RULESET_THIEU_THONG_TIN", "Mã và tên phiên bản mới là bắt buộc.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var x = new RuleSet { MaRuleSet = maMoi.Trim(), TenRuleSet = tenMoi.Trim(), CanCuPhapLy = source.CanCuPhapLy,
            MaxTCChuaTichLuy = source.MaxTCChuaTichLuy, GpaBaoVeMin = source.GpaBaoVeMin, GpaTotNghiepMin = source.GpaTotNghiepMin,
            DiemDatDATN = source.DiemDatDATN, TrongSoQT = source.TrongSoQT, HeSoThoiGianToiDa = source.HeSoThoiGianToiDa,
            MinTVHoiDong = source.MinTVHoiDong, MaxTVHoiDong = source.MaxTVHoiDong, NguongHaHangHocLaiPct = source.NguongHaHangHocLaiPct,
            HaHangKhiKyLuat = source.HaHangKhiKyLuat, HieuLucTu = source.HieuLucTu, HieuLucDen = source.HieuLucDen };
        db.RuleSet.Add(x);
        await db.SaveChangesAsync(ct);
        var bm = await db.ThamSoBoMon.AsNoTracking().Where(p => p.RuleSetId == id).ToListAsync(ct);
        db.ThamSoBoMon.AddRange(bm.Select(p => new ThamSoBoMon { BoMonId = p.BoMonId, RuleSetId = x.Id, TrongSoQT = p.TrongSoQT,
            TrongSoHD = p.TrongSoHD, TachDiemKhac = p.TachDiemKhac, SoNgaySuaGapToiDa = p.SoNgaySuaGapToiDa, DungHanKhiNghiTamThoi = p.DungHanKhiNghiTamThoi }));
        var plagiarism = await db.ThamSoDaoVan.AsNoTracking().Where(p => p.RuleSetId == id).ToListAsync(ct);
        db.ThamSoDaoVan.AddRange(plagiarism.Select(p => new ThamSoDaoVan { KhoaId = p.KhoaId, RuleSetId = x.Id, ChuongTrinh = p.ChuongTrinh, Bat = p.Bat, NguongPhanTram = p.NguongPhanTram }));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await LayAsync(x.Id, ct);
    }

    public async Task<IReadOnlyList<RuleSetKhoaHocRequest>> LayKhoaHocAsync(long id, CancellationToken ct = default) =>
        await db.RuleSetKhoaHoc.AsNoTracking().Where(x => x.RuleSetId == id).OrderBy(x => x.KhoaHocTu)
            .Select(x => new RuleSetKhoaHocRequest(x.RuleSetId, x.KhoaHocTu, x.KhoaHocDen, x.CanXacNhan)).ToListAsync(ct);

    public async Task GanKhoaHocAsync(long id, IReadOnlyList<RuleSetKhoaHocRequest> items, CancellationToken ct = default)
    {
        _ = await TimRule(id, ct);
        await KiemTraChuaDung(id, ct);
        if (items.Any(x => x.RuleSetId != id || x.KhoaHocTu < 1 || (x.KhoaHocDen is not null && x.KhoaHocDen < x.KhoaHocTu)))
            throw new DomainException("UPM_RULESET_KHOA_HOC_KHONG_HOP_LE", "Khoảng khóa học không hợp lệ.");
        var sorted = items.OrderBy(x => x.KhoaHocTu).ToArray();
        for (var i = 1; i < sorted.Length; i++)
            if (sorted[i - 1].KhoaHocDen is null || sorted[i].KhoaHocTu <= sorted[i - 1].KhoaHocDen)
                throw new DomainException("UPM_RULESET_KHOA_HOC_BI_CHONG_LAN", "Các khoảng khóa học trong RuleSet bị chồng lấn.");
        var all = await db.RuleSetKhoaHoc.Where(x => x.RuleSetId != id).ToListAsync(ct);
        foreach (var row in sorted)
        foreach (var assigned in all)
            if (GiaoNhau(row.KhoaHocTu, row.KhoaHocDen, assigned.KhoaHocTu, assigned.KhoaHocDen))
                throw new DomainException("UPM_RULESET_KHOA_HOC_DA_GAN", $"Khóa học {row.KhoaHocTu}–{row.KhoaHocDen?.ToString(CultureInfo.InvariantCulture) ?? "∞"} đã được gán cho RuleSet khác.");
        db.RuleSetKhoaHoc.RemoveRange(db.RuleSetKhoaHoc.Where(x => x.RuleSetId == id));
        db.RuleSetKhoaHoc.AddRange(sorted.Select(x => new RuleSetKhoaHoc { RuleSetId = id, KhoaHocTu = x.KhoaHocTu, KhoaHocDen = x.KhoaHocDen, CanXacNhan = x.CanXacNhan }));
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ThamSoBoMonItem>> LayThamSoBoMonAsync(long ruleSetId, CancellationToken ct = default)
    {
        var q = await scope.LocAsync(db.ThamSoBoMon.AsNoTracking(), ct: ct);
        return await q.Where(x => x.RuleSetId == ruleSetId).OrderBy(x => x.BoMonId)
            .Select(x => new ThamSoBoMonItem(x.Id, x.BoMonId, x.RuleSetId, x.TrongSoQT, x.TrongSoHD, x.TachDiemKhac, x.SoNgaySuaGapToiDa, x.DungHanKhiNghiTamThoi)).ToListAsync(ct);
    }

    public async Task<ThamSoBoMonItem> LuuThamSoBoMonAsync(ThamSoBoMonRequest request, CancellationToken ct = default)
    {
        var r = request;
        if (r.TrongSoQT is < 0 or > 1 || r.TrongSoHD is < 0 or > 1 || r.TrongSoQT + r.TrongSoHD != 1m)
            throw new DomainException("UPM_RULESET_TRONG_SO_KHONG_HOP_LE", "Trọng số QT và HĐ phải không âm và có tổng bằng 1.");
        if (r.SoNgaySuaGapToiDa is < 1 or > 3) throw new DomainException("UPM_RULESET_SO_NGAY_SUA_GAP_KHONG_HOP_LE", "Số ngày sửa gấp phải từ 1 đến 3.");
        var bm = await scope.LocAsync(db.BoMon.AsQueryable(), ct: ct);
        if (!await bm.AnyAsync(x => x.Id == r.BoMonId, ct)) throw new DomainException("UPM_DANH_MUC_NOT_FOUND", "Không tìm thấy bộ môn trong phạm vi của bạn.");
        _ = await TimRule(r.RuleSetId, ct);
        await KiemTraChuaDung(r.RuleSetId, ct);
        var p = await db.ThamSoBoMon.SingleOrDefaultAsync(x => x.BoMonId == r.BoMonId && x.RuleSetId == r.RuleSetId, ct);
        if (p is null) { p = new ThamSoBoMon { BoMonId = r.BoMonId, RuleSetId = r.RuleSetId }; db.ThamSoBoMon.Add(p); }
        p.TrongSoQT = r.TrongSoQT; p.TrongSoHD = r.TrongSoHD; p.TachDiemKhac = r.TachDiemKhac; p.SoNgaySuaGapToiDa = r.SoNgaySuaGapToiDa; p.DungHanKhiNghiTamThoi = r.DungHanKhiNghiTamThoi;
        await db.SaveChangesAsync(ct);
        return new ThamSoBoMonItem(p.Id, p.BoMonId, p.RuleSetId, p.TrongSoQT, p.TrongSoHD, p.TachDiemKhac, p.SoNgaySuaGapToiDa, p.DungHanKhiNghiTamThoi);
    }

    public async Task<IReadOnlyList<ThamSoDaoVanItem>> LayThamSoDaoVanAsync(long khoaId, CancellationToken ct = default)
    {
        var q = await scope.LocAsync(db.ThamSoDaoVan.AsNoTracking(), ct: ct);
        return await q.Where(x => x.KhoaId == khoaId).OrderBy(x => x.ChuongTrinh)
            .Select(x => new ThamSoDaoVanItem(x.Id, x.KhoaId, x.RuleSetId, x.ChuongTrinh.ToString(), x.Bat, x.NguongPhanTram)).ToListAsync(ct);
    }

    public async Task<ThamSoDaoVanItem> LuuThamSoDaoVanAsync(ThamSoDaoVanRequest request, CancellationToken ct = default)
    {
        var r = request;
        if (r.NguongPhanTram is < 20 or > 25) throw new DomainException("UPM_RULESET_NGUONG_DAO_VAN_KHONG_HOP_LE", "Ngưỡng đạo văn phải từ 20% đến 25%.");
        if (!Enum.TryParse<LoaiChuongTrinh>(r.ChuongTrinh, true, out var chuongTrinh)) throw new DomainException("UPM_RULESET_CHUONG_TRINH_KHONG_HOP_LE", "Chương trình không hợp lệ.");
        var khoa = await scope.LocAsync(db.Khoa.AsQueryable(), ct: ct);
        if (!await khoa.AnyAsync(x => x.Id == r.KhoaId, ct)) throw new DomainException("UPM_DANH_MUC_NOT_FOUND", "Không tìm thấy khoa trong phạm vi của bạn.");
        if (!await db.ChuongTrinhDaoTao.AnyAsync(x => x.KhoaId == r.KhoaId && x.ChuongTrinh == chuongTrinh, ct))
            throw new DomainException("UPM_RULESET_CHUONG_TRINH_KHONG_THUOC_KHOA", "Khoa chưa có chương trình đào tạo thuộc loại chương trình đã chọn.");
        if (r.RuleSetId is { } rule)
        {
            _ = await TimRule(rule, ct);
            await KiemTraChuaDung(rule, ct);
        }
        else
        {
            var daCoXet = await db.XetDieuKien.AnyAsync(ct) || await db.DiemDATN.AnyAsync(ct);
            RuleSetPolicy.KiemTraCoTheSua(daCoXet);
        }
        var p = await db.ThamSoDaoVan.SingleOrDefaultAsync(x => x.KhoaId == r.KhoaId && x.ChuongTrinh == chuongTrinh, ct);
        if (p is null) { p = new ThamSoDaoVan { KhoaId = r.KhoaId, ChuongTrinh = chuongTrinh }; db.ThamSoDaoVan.Add(p); }
        p.RuleSetId = r.RuleSetId; p.Bat = r.Bat; p.NguongPhanTram = r.NguongPhanTram;
        await db.SaveChangesAsync(ct);
        return new ThamSoDaoVanItem(p.Id, p.KhoaId, p.RuleSetId, p.ChuongTrinh.ToString(), p.Bat, p.NguongPhanTram);
    }

    private async Task<List<RuleSetItem>> Map(IReadOnlyList<RuleSet> rows, CancellationToken ct)
    {
        var ids = rows.Select(x => x.Id).ToArray();
        var used = await db.XetDieuKien.AsNoTracking().Where(x => ids.Contains(x.RuleSetId)).Select(x => x.RuleSetId)
            .Union(db.DiemDATN.AsNoTracking().Where(x => ids.Contains(x.RuleSetId)).Select(x => x.RuleSetId)).Distinct().ToListAsync(ct);
        var usedSet = used.ToHashSet();
        return rows.Select(x => new RuleSetItem(x.Id, x.MaRuleSet, x.TenRuleSet, x.CanCuPhapLy, x.MaxTCChuaTichLuy, x.GpaBaoVeMin,
            x.GpaTotNghiepMin, x.DiemDatDATN, x.TrongSoQT, x.HeSoThoiGianToiDa, x.MinTVHoiDong, x.MaxTVHoiDong,
            x.NguongHaHangHocLaiPct, x.HaHangKhiKyLuat, x.HieuLucTu, x.HieuLucDen, usedSet.Contains(x.Id))).ToList();
    }

    private async Task<RuleSet> TimRule(long id, CancellationToken ct) =>
        await db.RuleSet.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new DomainException("UPM_RULESET_NOT_FOUND", "Không tìm thấy RuleSet.");
    private async Task KiemTraChuaDung(long id, CancellationToken ct)
    {
        var used = await db.XetDieuKien.AnyAsync(x => x.RuleSetId == id, ct) || await db.DiemDATN.AnyAsync(x => x.RuleSetId == id, ct);
        RuleSetPolicy.KiemTraCoTheSua(used);
    }
    private static RuleSet Tao(RuleSetRequest r) { var x = new RuleSet(); Salin(x, r); return x; }
    private static void Salin(RuleSet x, RuleSetRequest r)
    {
        x.MaRuleSet = r.MaRuleSet.Trim(); x.TenRuleSet = r.TenRuleSet.Trim(); x.CanCuPhapLy = r.CanCuPhapLy;
        x.MaxTCChuaTichLuy = r.MaxTCChuaTichLuy; x.GpaBaoVeMin = r.GpaBaoVeMin; x.GpaTotNghiepMin = r.GpaTotNghiepMin;
        x.DiemDatDATN = r.DiemDatDATN; x.TrongSoQT = r.TrongSoQT; x.HeSoThoiGianToiDa = r.HeSoThoiGianToiDa;
        x.MinTVHoiDong = r.MinTVHoiDong; x.MaxTVHoiDong = r.MaxTVHoiDong; x.NguongHaHangHocLaiPct = r.NguongHaHangHocLaiPct;
        x.HaHangKhiKyLuat = r.HaHangKhiKyLuat; x.HieuLucTu = r.HieuLucTu; x.HieuLucDen = r.HieuLucDen;
    }
    private static void KiemTra(RuleSetRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.MaRuleSet) || string.IsNullOrWhiteSpace(r.TenRuleSet) || r.MaxTCChuaTichLuy < 0 ||
            r.GpaBaoVeMin is < 0 or > 4 || r.GpaTotNghiepMin is < 0 or > 4 || r.DiemDatDATN is < 0 or > 10 ||
            r.TrongSoQT is < 0 or > 1 || r.HeSoThoiGianToiDa <= 0 || r.MinTVHoiDong < 1 || r.MaxTVHoiDong < r.MinTVHoiDong ||
            r.NguongHaHangHocLaiPct is < 0 or > 100 || (r.HieuLucDen is { } den && den < r.HieuLucTu))
            throw new DomainException("UPM_RULESET_THAM_SO_KHONG_HOP_LE", "Thông tin hoặc tham số RuleSet không hợp lệ.");
    }
    private static bool GiaoNhau(int tu1, int? den1, int tu2, int? den2) => tu1 <= (den2 ?? int.MaxValue) && tu2 <= (den1 ?? int.MaxValue);
}
