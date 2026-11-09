using Microsoft.EntityFrameworkCore;
using Upm.Application.Abstractions;
using Upm.Application.Common;
using Upm.Application.PhanQuyen;
using Upm.Application.Workflow;
using Upm.Domain.Common;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Domain.PhanQuyen;
using Upm.Domain.Rules;
using Upm.Domain.Workflow;
using Upm.Infrastructure.Persistence;

namespace Upm.Infrastructure.Workflow;

/// <summary>UC-1: đợt ĐATN, hồ sơ SV trong đợt, kết quả xét giao, khiếu nại (A20), quyết định giao (T03).</summary>
public sealed class DotDATNService(
    UpmDbContext db,
    IScopeFilter scope,
    IQuyenHienTai quyen,
    ICurrentUser currentUser,
    IAuditContext auditContext,
    IClock clock,
    IWorkflowService workflow,
    IXetGiaoBatchRunner xetGiao) : IDotDATNService
{
    /// <summary>Giá trị lọc "chưa xét" cho cột kết quả xét giao.</summary>
    public const string ChuaXet = "CHUA_XET";

    private static readonly TrangThaiSVTrongDot[] TrangThaiCoTheLamLai =
        [TrangThaiSVTrongDot.PHAI_LAM_LAI, TrangThaiSVTrongDot.KHONG_DU_DK];

    // ------------------------------------------------------------------ Đợt

    public async Task<PagedResult<DotDATNItem>> DanhSachDotAsync(string? q, int page, int pageSize, CancellationToken ct = default)
    {
        var query = await scope.LocAsync(db.DotDATN.AsNoTracking(), ct: ct);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.MaDot.Contains(term) || x.TenDot.Contains(term) || x.NamHoc.Contains(term));
        }
        var request = new PageRequest(page, pageSize);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.NgayBatDau).ThenBy(x => x.MaDot)
            .Skip(request.Skip).Take(request.SafePageSize)
            .Select(x => new { Dot = x, SoHoSo = db.SVTrongDot.Count(s => s.DotId == x.Id) })
            .ToListAsync(ct);
        return new PagedResult<DotDATNItem>(rows.Select(x => ToItem(x.Dot, x.SoHoSo)).ToArray(),
            request.SafePage, request.SafePageSize, total);
    }

    public async Task<DotDATNItem> LayDotAsync(long id, CancellationToken ct = default)
    {
        var dot = await LayDotBatBuocAsync(id, ct);
        return ToItem(dot, await db.SVTrongDot.CountAsync(x => x.DotId == id, ct));
    }

    public async Task<DotDATNItem> TaoDotAsync(DotDATNRequest request, CancellationToken ct = default)
    {
        KiemTraDot(request);
        var permissions = await quyen.LayAsync(ct);
        if (!permissions.CoSoDuocPhep.Contains(request.CoSoId))
            throw Loi("UPM_DOT_SCOPE_DENIED", "Bạn không có quyền quản lý đợt tại cơ sở này.");
        var maDot = request.MaDot.Trim();
        if (await db.DotDATN.AnyAsync(x => x.MaDot == maDot, ct))
            throw Loi("UPM_DOT_MA_TRUNG", $"Mã đợt {maDot} đã tồn tại.");

        var dot = new DotDATN
        {
            CoSoId = request.CoSoId,
            MaDot = maDot,
            TenDot = request.TenDot.Trim(),
            NamHoc = request.NamHoc.Trim(),
            HocKy = request.HocKy,
            NgayBatDau = request.NgayBatDau,
            SoTuan = request.SoTuan,
            NgayNopQuyenHan = request.NgayNopQuyenHan,
            TrangThai = TrangThaiDot.Mo,
        };
        db.DotDATN.Add(dot);
        await db.SaveChangesAsync(ct);
        // UC-3a: 3 mốc tiến độ tự sinh khi tạo đợt (sửa được sau đó).
        db.MocTienDo.AddRange(SinhMocMacDinh(dot));
        await db.SaveChangesAsync(ct);
        return ToItem(dot, 0);
    }

    public async Task<DotDATNItem> SuaDotAsync(long id, DotDATNRequest request, CancellationToken ct = default)
    {
        KiemTraDot(request);
        var dot = await LayDotBatBuocAsync(id, ct);
        if (dot.TrangThai == TrangThaiDot.Dong)
            throw Loi("UPM_DOT_DA_DONG", "Đợt đã đóng nên không thể sửa.");
        if (dot.CoSoId != request.CoSoId)
            throw Loi("UPM_DOT_KHONG_DOI_CO_SO", "Không thể chuyển đợt sang cơ sở khác.");
        var maDot = request.MaDot.Trim();
        if (await db.DotDATN.AnyAsync(x => x.MaDot == maDot && x.Id != id, ct))
            throw Loi("UPM_DOT_MA_TRUNG", $"Mã đợt {maDot} đã tồn tại.");
        var soHoSo = await db.SVTrongDot.CountAsync(x => x.DotId == id, ct);
        // Kỳ xét (năm học + học kỳ) quyết định "đúng tiến độ" (A06/A07) – không đổi khi đã có kết quả xét.
        if ((dot.NamHoc != request.NamHoc.Trim() || dot.HocKy != request.HocKy)
            && await db.XetDieuKien.AnyAsync(x => x.SVTrongDot.DotId == id, ct))
            throw Loi("UPM_DOT_DA_XET_KHONG_DOI_KY", "Đợt đã có kết quả xét giao: không đổi năm học / học kỳ (ảnh hưởng xét đúng tiến độ).");

        dot.MaDot = maDot;
        dot.TenDot = request.TenDot.Trim();
        dot.NamHoc = request.NamHoc.Trim();
        dot.HocKy = request.HocKy;
        dot.NgayBatDau = request.NgayBatDau;
        dot.SoTuan = request.SoTuan;
        dot.NgayNopQuyenHan = request.NgayNopQuyenHan;
        await db.SaveChangesAsync(ct);
        return ToItem(dot, soHoSo);
    }

    public async Task<DotDATNItem> DongDotAsync(long id, CancellationToken ct = default)
    {
        var dot = await LayDotBatBuocAsync(id, ct);
        if (dot.TrangThai == TrangThaiDot.Dong)
            throw Loi("UPM_DOT_DA_DONG", "Đợt đã đóng.");
        dot.TrangThai = TrangThaiDot.Dong;
        await db.SaveChangesAsync(ct);
        return ToItem(dot, await db.SVTrongDot.CountAsync(x => x.DotId == id, ct));
    }

    // ------------------------------------------------------------------ Hồ sơ SV trong đợt

    public async Task<DanhSachHoSoKetQua> DanhSachHoSoAsync(long dotId, LocHoSoRequest loc, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(loc);
        _ = await LayDotBatBuocAsync(dotId, ct);
        var all = (await scope.LocAsync(db.SVTrongDot.AsNoTracking(), ct: ct)).Where(x => x.DotId == dotId);
        var thongKe = await all.GroupBy(x => x.TrangThai)
            .Select(g => new ThongKeTrangThaiItem(g.Key, g.Count())).ToListAsync(ct);

        var query = all;
        if (loc.TrangThai is { } trangThai) query = query.Where(x => x.TrangThai == trangThai);
        if (loc.CoNoTinChi is { } coNo) query = query.Where(x => x.CoNoTinChi == coNo);
        if (loc.ChuaQuaTTTN is { } chuaTTTN) query = query.Where(x => x.ChuaQuaTTTN == chuaTTTN);
        if (!string.IsNullOrWhiteSpace(loc.KetQuaXet))
        {
            var ketQua = loc.KetQuaXet.Trim();
            query = ketQua == ChuaXet
                ? query.Where(x => !db.XetDieuKien.Any(k => k.SVTrongDotId == x.Id && (k.LoaiXet == LoaiXet.Giao || k.LoaiXet == LoaiXet.XetLai)))
                : query.Where(x => db.XetDieuKien
                    .Where(k => k.SVTrongDotId == x.Id && (k.LoaiXet == LoaiXet.Giao || k.LoaiXet == LoaiXet.XetLai))
                    .OrderByDescending(k => k.ThoiDiemXet).ThenByDescending(k => k.Id)
                    .Select(k => k.KetQua).FirstOrDefault() == ketQua);
        }
        if (!string.IsNullOrWhiteSpace(loc.Q))
        {
            var term = loc.Q.Trim();
            query = query.Where(x => x.SinhVien.MaSV.Contains(term) || x.SinhVien.HoTen.Contains(term)
                || x.CTDT.MaCTDT.Contains(term) || x.CTDT.TenCTDT.Contains(term));
        }

        var page = new PageRequest(loc.Page, loc.PageSize);
        var total = await query.CountAsync(ct);
        var rows = await ChieuHoSo(query.OrderBy(x => x.SinhVien.MaSV).ThenBy(x => x.CTDT.MaCTDT)
            .Skip(page.Skip).Take(page.SafePageSize)).ToListAsync(ct);
        return new DanhSachHoSoKetQua(rows.Select(ToHoSoItem).ToArray(), page.SafePage, page.SafePageSize, total,
            thongKe.OrderBy(x => x.TrangThai).ToArray());
    }

    public async Task<IReadOnlyList<SinhVienUngVienItem>> TimSinhVienUngVienAsync(
        long dotId, int? khoaHoc, string? nganh, string? q, CancellationToken ct = default)
    {
        var dot = await LayDotBatBuocAsync(dotId, ct);
        var rows = await TruyVanUngVienAsync(dot, khoaHoc, nganh, q, ct);
        var lamLai = await HoSoLamLaiGanNhatAsync(dot, rows.Select(x => x.SinhVienId).ToArray(), ct);
        return rows.Select(x =>
        {
            var lyDo = LyDoKhongDuaVaoDot(x.TrangThaiHocTap);
            lamLai.TryGetValue((x.SinhVienId, x.CTDTId), out var hoSoTruoc);
            return x with { CoTheThem = lyDo is null, LyDoKhongThem = lyDo, HoSoTruocId = hoSoTruoc?.Id };
        }).ToArray();
    }

    public async Task<ThemHoSoKetQua> ThemHoSoAsync(long dotId, IReadOnlyList<HoSoTrongDotRequest> items, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0) throw Loi("UPM_DOT_HO_SO_RONG", "Cần chọn ít nhất một hồ sơ sinh viên.");
        if (items.Select(x => (x.SinhVienId, x.CTDTId)).Distinct().Count() != items.Count)
            throw Loi("UPM_DOT_HO_SO_TRUNG_TRONG_YEU_CAU", "Danh sách yêu cầu có hồ sơ bị lặp.");
        var dot = await LayDotMoAsync(dotId, ct);

        var studentQuery = await scope.LocAsync(db.SinhVien.AsNoTracking(), ct: ct);
        var programQuery = await scope.LocAsync(db.ChuongTrinhDaoTao.AsNoTracking(), ct: ct);
        var prepared = new List<(SinhVien SinhVien, long CTDTId, long? HoSoTruocId)>();
        foreach (var item in items)
        {
            var student = await studentQuery.SingleOrDefaultAsync(x => x.Id == item.SinhVienId, ct)
                ?? throw Loi("UPM_DOT_SINH_VIEN_NOT_FOUND", $"Không tìm thấy sinh viên {item.SinhVienId} trong phạm vi được phép.");
            var program = await programQuery.Select(x => new { x.Id, x.Khoa.CoSoId }).SingleOrDefaultAsync(x => x.Id == item.CTDTId, ct)
                ?? throw Loi("UPM_DOT_CTDT_NOT_FOUND", $"Không tìm thấy CTĐT {item.CTDTId} trong phạm vi được phép.");
            if (student.CoSoId != dot.CoSoId || program.CoSoId != dot.CoSoId)
                throw Loi("UPM_DOT_CO_SO_KHONG_KHOP", $"Sinh viên {student.MaSV} và CTĐT phải thuộc cơ sở của đợt.");
            if (LyDoKhongDuaVaoDot(student.TrangThaiHocTap) is { } lyDo)
                throw Loi(MaLoiKhongDuaVaoDot(student.TrangThaiHocTap), $"Sinh viên {student.MaSV}: {lyDo}");
            prepared.Add((student, program.Id, item.HoSoTruocId));
        }
        await ThemCoreAsync(dot, prepared, ct);
        return new ThemHoSoKetQua(prepared.Count, []);
    }

    public async Task<ThemHoSoKetQua> ThemHoSoTheoLocAsync(long dotId, ThemHoSoTheoLocRequest loc, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(loc);
        if (loc.KhoaHoc is null && string.IsNullOrWhiteSpace(loc.Nganh) && string.IsNullOrWhiteSpace(loc.Q))
            throw Loi("UPM_DOT_LOC_RONG", "Cần chọn ít nhất một điều kiện lọc (khóa, ngành hoặc từ khóa).");
        var dot = await LayDotMoAsync(dotId, ct);
        var candidates = await TruyVanUngVienAsync(dot, loc.KhoaHoc, loc.Nganh, loc.Q, ct);
        var boQua = new List<ThemHoSoBoQuaItem>();
        var prepared = new List<(SinhVien SinhVien, long CTDTId, long? HoSoTruocId)>();
        var students = await db.SinhVien.AsNoTracking()
            .Where(x => candidates.Select(c => c.SinhVienId).Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        foreach (var candidate in candidates)
        {
            if (LyDoKhongDuaVaoDot(candidate.TrangThaiHocTap) is { } lyDo)
            {
                boQua.Add(new ThemHoSoBoQuaItem(candidate.MaSV, candidate.HoTen, lyDo));
                continue;
            }
            prepared.Add((students[candidate.SinhVienId], candidate.CTDTId, null));
        }
        if (prepared.Count > 0) await ThemCoreAsync(dot, prepared, ct);
        return new ThemHoSoKetQua(prepared.Count, boQua);
    }

    /// <summary>
    /// Tạo SVTrongDot CHO_XET (UK DotId + SinhVienId + CTDTId; học 2 CTĐT = 2 hồ sơ – A18). Hồ sơ làm lại (A21) dựng
    /// bằng <see cref="DATNWorkflow.TaoHoSoLamLai"/> để liên kết HoSoTruocId và tăng LanLamLai (tính là học lại).
    /// </summary>
    private async Task ThemCoreAsync(DotDATN dot, IReadOnlyList<(SinhVien SinhVien, long CTDTId, long? HoSoTruocId)> items, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var sinhVienIds = items.Select(x => x.SinhVien.Id).Distinct().ToArray();
        var daCo = (await db.SVTrongDot.Where(x => x.DotId == dot.Id && sinhVienIds.Contains(x.SinhVienId))
            .Select(x => new { x.SinhVienId, x.CTDTId }).ToListAsync(ct)).Select(x => (x.SinhVienId, x.CTDTId)).ToHashSet();
        var lamLai = await HoSoLamLaiGanNhatAsync(dot, sinhVienIds, ct);
        var now = clock.Now.UtcDateTime;
        var created = new List<SVTrongDot>();
        foreach (var (student, ctdtId, hoSoTruocId) in items)
        {
            if (daCo.Contains((student.Id, ctdtId)))
                throw Loi("UPM_DOT_HO_SO_DA_TON_TAI", $"Sinh viên {student.MaSV} đã có hồ sơ CTĐT này trong đợt.");

            SVTrongDot? truoc;
            if (hoSoTruocId is not null)
            {
                truoc = await db.SVTrongDot.AsNoTracking().Include(x => x.Dot).SingleOrDefaultAsync(x => x.Id == hoSoTruocId, ct);
                if (truoc is null || truoc.SinhVienId != student.Id || truoc.CTDTId != ctdtId || truoc.CoSoId != dot.CoSoId
                    || truoc.Dot.NgayBatDau >= dot.NgayBatDau || !TrangThaiCoTheLamLai.Contains(truoc.TrangThai))
                    throw Loi("UPM_A21_HO_SO_TRUOC_KHONG_HOP_LE",
                        "Hồ sơ làm lại phải cùng sinh viên/CTĐT, thuộc đợt trước và đã kết thúc PHAI_LAM_LAI hoặc KHONG_DU_DK.");
            }
            else
            {
                lamLai.TryGetValue((student.Id, ctdtId), out truoc);
            }

            var record = truoc is null
                ? new SVTrongDot { DotId = dot.Id, SinhVienId = student.Id, CTDTId = ctdtId, CoSoId = dot.CoSoId }
                : DATNWorkflow.TaoHoSoLamLai(truoc, dot.Id);
            created.Add(record);
            db.SVTrongDot.Add(record);
        }
        await db.SaveChangesAsync(ct);
        // Tạo hồ sơ không phải chuyển trạng thái (chưa có trạng thái trước) – chỉ ghi mốc đầu của lịch sử.
        db.LichSuTrangThai.AddRange(created.Select(x => new LichSuTrangThai
        {
            SVTrongDotId = x.Id,
            TuTrangThai = null,
            DenTrangThai = x.TrangThai,
            LyDo = x.HoSoTruocId is null
                ? "Thêm sinh viên vào đợt ĐATN."
                : $"A21: hồ sơ làm lại lần {x.LanLamLai}, liên kết hồ sơ {x.HoSoTruocId}.",
            NguoiThucHienId = currentUser.TaiKhoanId,
            ThoiDiem = now,
        }));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task<List<SinhVienUngVienItem>> TruyVanUngVienAsync(DotDATN dot, int? khoaHoc, string? nganh, string? q, CancellationToken ct)
    {
        var students = await scope.LocAsync(db.SinhVien.AsNoTracking(), ct: ct);
        var programs = await scope.LocAsync(db.ChuongTrinhDaoTao.AsNoTracking(), ct: ct);
        var query = from student in students
                    join program in programs on student.CTDTId equals program.Id
                    where student.CoSoId == dot.CoSoId
                        && !db.SVTrongDot.Any(x => x.DotId == dot.Id && x.SinhVienId == student.Id && x.CTDTId == program.Id)
                    select new { student, program };
        if (khoaHoc is not null) query = query.Where(x => x.student.KhoaHoc == khoaHoc);
        if (!string.IsNullOrWhiteSpace(nganh))
        {
            var term = nganh.Trim();
            query = query.Where(x => x.program.Nganh.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.student.MaSV.Contains(term) || x.student.HoTen.Contains(term)
                || x.program.MaCTDT.Contains(term) || x.program.TenCTDT.Contains(term));
        }
        return await query.OrderBy(x => x.student.MaSV).Take(1000).Select(x => new SinhVienUngVienItem(
            x.student.Id, x.student.MaSV, x.student.HoTen, x.student.KhoaHoc, x.program.Id,
            x.program.MaCTDT, x.program.TenCTDT, x.program.Nganh, x.student.TrangThaiHocTap, true, null, null)).ToListAsync(ct);
    }

    /// <summary>A21: hồ sơ PHAI_LAM_LAI/KHONG_DU_DK gần nhất (đợt trước, cùng cơ sở) chưa được hồ sơ nào làm lại.</summary>
    private async Task<Dictionary<(long SinhVienId, long CTDTId), SVTrongDot>> HoSoLamLaiGanNhatAsync(
        DotDATN dot, long[] sinhVienIds, CancellationToken ct)
    {
        if (sinhVienIds.Length == 0) return [];
        var rows = await db.SVTrongDot.AsNoTracking()
            .Where(x => sinhVienIds.Contains(x.SinhVienId) && x.CoSoId == dot.CoSoId && x.Dot.NgayBatDau < dot.NgayBatDau
                && TrangThaiCoTheLamLai.Contains(x.TrangThai)
                && !db.SVTrongDot.Any(n => n.HoSoTruocId == x.Id))
            .OrderByDescending(x => x.Dot.NgayBatDau).ThenByDescending(x => x.Id)
            .ToListAsync(ct);
        return rows.GroupBy(x => (x.SinhVienId, x.CTDTId)).ToDictionary(g => g.Key, g => g.First());
    }

    /// <summary>A11 – luật ở rule engine (<see cref="DuaVaoDot"/>); null = được đưa vào đợt.</summary>
    private static string? LyDoKhongDuaVaoDot(TrangThaiHocTap trangThai) =>
        DuaVaoDot.KiemTra(trangThai) is { Dat: false } ketQua ? ketQua.LyDo : null;

    private static string MaLoiKhongDuaVaoDot(TrangThaiHocTap trangThai) =>
        DuaVaoDot.KiemTra(trangThai).MaNgoaiLe == "A11" ? "UPM_A11_NGHI_TAM_THOI" : "UPM_DOT_SINH_VIEN_DA_TOT_NGHIEP";

    // ------------------------------------------------------------------ Snapshot, xét lại (A19)

    public async Task<IReadOnlyList<XetGiaoSnapshotItem>> LaySnapshotAsync(long svTrongDotId, CancellationToken ct = default)
    {
        var query = await scope.LocAsync(db.SVTrongDot.AsNoTracking(), ct: ct);
        if (!await query.AnyAsync(x => x.Id == svTrongDotId, ct))
            throw Loi("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ trong phạm vi được phép.");
        return await XetGiaoBatchRunner.DocSnapshotsAsync(db, SnapshotXetGiao(svTrongDotId), ct);
    }

    public async Task<XetGiaoSnapshotItem> XetLaiAsync(long svTrongDotId, CancellationToken ct = default)
    {
        await KiemTraQuyenAsync(Quyen.XetDieuKienChay, ct);
        await KiemTraChuaCoQuyetDinhAsync(svTrongDotId, ct);
        return await xetGiao.XetLaiAsync(svTrongDotId, currentUser.TaiKhoanId, ct);
    }

    // ------------------------------------------------------------------ Sinh viên: kết quả + khiếu nại (A20)

    public async Task<IReadOnlyList<HoSoCuaToiXetGiaoItem>> LayKetQuaCuaToiAsync(CancellationToken ct = default)
    {
        var sinhVienId = SinhVienHienTai();
        // Phạm vi của SV = chính hồ sơ của mình (lọc theo SinhVienId, không theo cơ sở/khoa).
        var hoSos = await db.SVTrongDot.AsNoTracking().Where(x => x.SinhVienId == sinhVienId)
            .OrderByDescending(x => x.Dot.NgayBatDau).ThenBy(x => x.CTDT.MaCTDT)
            .Select(x => new
            {
                x.Id,
                x.DotId,
                x.Dot.MaDot,
                x.Dot.TenDot,
                x.CTDT.MaCTDT,
                x.CTDT.TenCTDT,
                x.TrangThai,
                DaCoQD = x.QuyetDinhGiaoId != null,
            }).ToListAsync(ct);
        var ids = hoSos.Select(x => x.Id).ToArray();
        var snapshots = (await XetGiaoBatchRunner.DocSnapshotsAsync(db, db.XetDieuKien
                .Where(x => ids.Contains(x.SVTrongDotId) && (x.LoaiXet == LoaiXet.Giao || x.LoaiXet == LoaiXet.XetLai)), ct))
            .ToList();
        var snapshotHoSo = await db.XetDieuKien.AsNoTracking().Where(x => ids.Contains(x.SVTrongDotId))
            .Select(x => new { x.Id, x.SVTrongDotId }).ToDictionaryAsync(x => x.Id, x => x.SVTrongDotId, ct);
        var khieuNai = await KhieuNaiQuery(db.DonTu.Where(x => ids.Contains(x.SVTrongDotId))).ToListAsync(ct);

        return hoSos.Select(h =>
        {
            var moiNhat = snapshots.LastOrDefault(s => snapshotHoSo[s.Id] == h.Id);
            var donCuaHoSo = khieuNai.Where(k => k.SVTrongDotId == h.Id).ToArray();
            var coTheKhieuNai = moiNhat is not null && !h.DaCoQD && donCuaHoSo.All(k => k.TrangThai != TrangThaiDonTu.ChoXuLy);
            return new HoSoCuaToiXetGiaoItem(h.Id, h.DotId, h.MaDot, h.TenDot, h.MaCTDT, h.TenCTDT, h.TrangThai, h.DaCoQD,
                coTheKhieuNai, moiNhat, donCuaHoSo);
        }).ToArray();
    }

    public async Task<long> TaoKhieuNaiAsync(long svTrongDotId, DonKhieuNaiRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.NoiDung) || request.NoiDung.Trim().Length > 4000)
            throw Loi("UPM_KHIEU_NAI_NOI_DUNG_KHONG_HOP_LE", "Nội dung khiếu nại phải có từ 1 đến 4.000 ký tự.");
        var sinhVienId = SinhVienHienTai();
        var hoSo = await db.SVTrongDot.SingleOrDefaultAsync(x => x.Id == svTrongDotId && x.SinhVienId == sinhVienId, ct)
            ?? throw Loi("UPM_KHIEU_NAI_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ của sinh viên hiện tại.");
        if (!await SnapshotXetGiao(hoSo.Id).AnyAsync(ct))
            throw Loi("UPM_KHIEU_NAI_CHUA_CO_KET_QUA", "Hồ sơ chưa có kết quả xét giao để khiếu nại.");
        if (hoSo.QuyetDinhGiaoId is not null)
            throw Loi("UPM_KHIEU_NAI_DA_CO_QUYET_DINH", "Đã có quyết định giao ĐATN; kết quả xét không còn khiếu nại trên hệ thống.");
        if (await db.DonTu.AnyAsync(x => x.SVTrongDotId == hoSo.Id && x.LoaiDon == LoaiDon.KhieuNaiXetGiao
                && x.TrangThai == TrangThaiDonTu.ChoXuLy, ct))
            throw Loi("UPM_KHIEU_NAI_DANG_CHO_XU_LY", "Hồ sơ đã có khiếu nại đang chờ xử lý.");
        var don = new DonTu
        {
            SVTrongDotId = hoSo.Id,
            LoaiDon = LoaiDon.KhieuNaiXetGiao,
            NoiDung = request.NoiDung.Trim(),
            FileMinhChungId = request.FileMinhChungId,
            NgayGui = clock.Now.UtcDateTime,
            TrangThai = TrangThaiDonTu.ChoXuLy,
        };
        db.DonTu.Add(don);
        await db.SaveChangesAsync(ct);
        return don.Id;
    }

    public async Task<IReadOnlyList<KhieuNaiItem>> DanhSachKhieuNaiAsync(long dotId, TrangThaiDonTu? trangThai, CancellationToken ct = default)
    {
        _ = await LayDotBatBuocAsync(dotId, ct);
        var query = (await scope.LocAsync(db.DonTu.AsNoTracking(), ct: ct)).Where(x => x.SVTrongDot.DotId == dotId);
        if (trangThai is not null) query = query.Where(x => x.TrangThai == trangThai);
        return await KhieuNaiQuery(query).ToListAsync(ct);
    }

    public async Task XuLyKhieuNaiAsync(long donTuId, XuLyKhieuNaiRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await KiemTraQuyenAsync(Quyen.XetDieuKienChay, ct);
        if (!request.ChapNhan && string.IsNullOrWhiteSpace(request.LyDo))
            throw Loi("UPM_KHIEU_NAI_LY_DO_TU_CHOI_BAT_BUOC", "Khi từ chối khiếu nại phải ghi lý do.");
        if (request.LyDo?.Trim().Length > 900)
            throw Loi("UPM_KHIEU_NAI_LY_DO_QUA_DAI", "Lý do xử lý không được vượt quá 900 ký tự.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var don = await (await scope.LocAsync(db.DonTu.AsQueryable(), ct: ct))
            .SingleOrDefaultAsync(x => x.Id == donTuId && x.LoaiDon == LoaiDon.KhieuNaiXetGiao, ct)
            ?? throw Loi("UPM_KHIEU_NAI_NOT_FOUND", "Không tìm thấy đơn khiếu nại xét giao trong phạm vi được phép.");
        if (don.TrangThai != TrangThaiDonTu.ChoXuLy)
            throw Loi("UPM_KHIEU_NAI_DA_XU_LY", "Đơn khiếu nại đã được xử lý.");

        string ketQua;
        if (request.ChapNhan)
        {
            // A20 → A19: chấp nhận = xét lại với dữ liệu hiện tại (snapshot mới, giữ snapshot cũ).
            await KiemTraChuaCoQuyetDinhAsync(don.SVTrongDotId, ct);
            var snapshot = await xetGiao.XetLaiAsync(don.SVTrongDotId, currentUser.TaiKhoanId, ct);
            ketQua = $"Chấp nhận khiếu nại, đã xét lại: {snapshot.KetQua}." + (string.IsNullOrWhiteSpace(request.LyDo) ? "" : $" {request.LyDo.Trim()}");
            don.TrangThai = TrangThaiDonTu.DaDuyet;
        }
        else
        {
            ketQua = request.LyDo!.Trim();
            don.TrangThai = TrangThaiDonTu.TuChoi;
        }
        don.NguoiXuLyId = currentUser.TaiKhoanId;
        don.KetQuaXuLy = ketQua;
        don.NgayXuLy = clock.Now.UtcDateTime;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    // ------------------------------------------------------------------ Quyết định giao (T03)

    public async Task<QuyetDinhGiaoItem> GhiNhanQuyetDinhAsync(long dotId, QuyetDinhGiaoRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await KiemTraQuyenAsync(Quyen.QuyetDinhGhiNhan, ct);
        var dot = await LayDotBatBuocAsync(dotId, ct);
        if (dot.TrangThai == TrangThaiDot.Dong) throw Loi("UPM_DOT_DA_DONG", "Đợt đã đóng.");
        var soQD = request.SoQuyetDinh?.Trim() ?? string.Empty;
        if (soQD.Length is 0 or > 50)
            throw Loi("UPM_QUYET_DINH_SO_KHONG_HOP_LE", "Số quyết định phải có từ 1 đến 50 ký tự.");
        if (request.NguoiKy?.Trim().Length > 100)
            throw Loi("UPM_QUYET_DINH_NGUOI_KY_QUA_DAI", "Người ký không được vượt quá 100 ký tự.");
        if (request.SVTrongDotIds is null || request.SVTrongDotIds.Count == 0
            || request.SVTrongDotIds.Distinct().Count() != request.SVTrongDotIds.Count)
            throw Loi("UPM_QUYET_DINH_DANH_SACH_HO_SO_KHONG_HOP_LE", "Cần chọn danh sách hồ sơ không rỗng và không trùng.");
        if (await db.QuyetDinh.AnyAsync(x => x.CoSoId == dot.CoSoId && x.SoQuyetDinh == soQD, ct))
            throw Loi("UPM_QUYET_DINH_TRUNG_SO", $"Số quyết định {soQD} đã được ghi nhận tại cơ sở này.");

        var records = await db.SVTrongDot.Include(x => x.SinhVien)
            .Where(x => x.DotId == dotId && request.SVTrongDotIds.Contains(x.Id)).ToListAsync(ct);
        if (records.Count != request.SVTrongDotIds.Count)
            throw Loi("UPM_QUYET_DINH_HO_SO_KHONG_THUOC_DOT", "Có hồ sơ không thuộc đợt đã chọn.");
        foreach (var record in records)
        {
            if (record.TrangThai != TrangThaiSVTrongDot.CHO_XET || record.QuyetDinhGiaoId is not null)
                throw Loi("UPM_QUYET_DINH_TRANG_THAI_KHONG_HOP_LE",
                    $"Hồ sơ {record.SinhVien.MaSV} đang {record.TrangThai}; chỉ hồ sơ chờ xét chưa có quyết định được gắn quyết định giao.");
            var ketQua = await SnapshotXetGiao(record.Id).OrderByDescending(x => x.ThoiDiemXet).ThenByDescending(x => x.Id)
                .Select(x => x.KetQua).FirstOrDefaultAsync(ct);
            if (ketQua != nameof(KetLuanXetGiao.DU_DK))
                throw Loi("UPM_QUYET_DINH_CHUA_DU_DIEU_KIEN", $"Hồ sơ {record.SinhVien.MaSV} chưa có kết quả xét gần nhất là đủ điều kiện.");
            // T03 cần đề tài (UC-2 / Phần 8): chặn ngay để không tạo QĐ "treo".
            if (record.DeTaiId is null)
                throw Loi("UPM_QUYET_DINH_CHUA_CO_DE_TAI", $"Hồ sơ {record.SinhVien.MaSV} chưa có đề tài được duyệt.");
            // T03 (UC-2 · B05): đủ ĐK + có đề tài + có GVHD còn hiệu lực.
            if (!await db.PhanCong.AnyAsync(p => p.SVTrongDotId == record.Id && p.VaiTro == VaiTroPhanCong.GVHD && p.NgayKetThuc == null, ct))
                throw Loi("UPM_QUYET_DINH_CHUA_CO_GVHD", $"Hồ sơ {record.SinhVien.MaSV} chưa được phân công GVHD.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var decision = new QuyetDinh
        {
            CoSoId = dot.CoSoId,
            SoQuyetDinh = soQD,
            NgayKy = request.NgayKy,
            LoaiQD = LoaiQuyetDinh.GiaoDATN,
            FileScanId = request.FileScanId,
            NguoiKy = string.IsNullOrWhiteSpace(request.NguoiKy) ? null : request.NguoiKy.Trim(),
            // Khóa dữ liệu (CLAUDE.md §5.6): sau QĐ, không xét lại / khiếu nại kết quả giao của các hồ sơ này.
            DaKhoaDuLieu = true,
            GhiChu = $"Quyết định giao ĐATN đợt {dot.MaDot}.",
        };
        var oldDecision = auditContext.SoQDBatBuoc;
        try
        {
            auditContext.SoQDBatBuoc = soQD;
            db.QuyetDinh.Add(decision);
            await db.SaveChangesAsync(ct);
            foreach (var record in records)
            {
                record.QuyetDinhGiaoId = decision.Id;
                db.QuyetDinhDoiTuong.Add(new QuyetDinhDoiTuong { QuyetDinhId = decision.Id, SVTrongDotId = record.Id });
            }
            await db.SaveChangesAsync(ct);
            foreach (var record in records)
            {
                await workflow.TransitionAsync(record.Id, new WorkflowTransitionRequest(
                    Convert.ToBase64String(record.RowVersion), MaChuyen: "T03",
                    LyDo: $"Đủ điều kiện, có đề tài và quyết định giao số {soQD}.", QuyetDinhId: decision.Id), ct);
            }
            await tx.CommitAsync(ct);
        }
        finally
        {
            auditContext.SoQDBatBuoc = oldDecision;
        }
        return new QuyetDinhGiaoItem(decision.Id, decision.SoQuyetDinh, decision.NgayKy, decision.NguoiKy, decision.FileScanId,
            decision.DaKhoaDuLieu, records.Select(x => x.Id).Order().ToArray());
    }

    public async Task<IReadOnlyList<QuyetDinhGiaoItem>> DanhSachQuyetDinhGiaoAsync(long dotId, CancellationToken ct = default)
    {
        _ = await LayDotBatBuocAsync(dotId, ct);
        var rows = await db.QuyetDinh.AsNoTracking()
            .Where(x => x.LoaiQD == LoaiQuyetDinh.GiaoDATN && x.DoiTuongs.Any(d => d.SVTrongDot != null && d.SVTrongDot.DotId == dotId))
            .OrderByDescending(x => x.NgayKy).ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.SoQuyetDinh,
                x.NgayKy,
                x.NguoiKy,
                x.FileScanId,
                x.DaKhoaDuLieu,
                HoSo = x.DoiTuongs.Where(d => d.SVTrongDotId != null).Select(d => d.SVTrongDotId!.Value).ToList(),
            }).ToListAsync(ct);
        return rows.Select(x => new QuyetDinhGiaoItem(x.Id, x.SoQuyetDinh, x.NgayKy, x.NguoiKy, x.FileScanId, x.DaKhoaDuLieu,
            x.HoSo.Order().ToArray())).ToArray();
    }

    // ------------------------------------------------------------------ Tiện ích

    private IQueryable<XetDieuKien> SnapshotXetGiao(long svTrongDotId) =>
        db.XetDieuKien.Where(x => x.SVTrongDotId == svTrongDotId && (x.LoaiXet == LoaiXet.Giao || x.LoaiXet == LoaiXet.XetLai));

    private IQueryable<HoSoRow> ChieuHoSo(IQueryable<SVTrongDot> query) => query.Select(x => new HoSoRow
    {
        Id = x.Id,
        SinhVienId = x.SinhVienId,
        MaSV = x.SinhVien.MaSV,
        HoTen = x.SinhVien.HoTen,
        KhoaHoc = x.SinhVien.KhoaHoc,
        MaCTDT = x.CTDT.MaCTDT,
        TenCTDT = x.CTDT.TenCTDT,
        TrangThai = x.TrangThai,
        CoNoTinChi = x.CoNoTinChi,
        ChuaQuaTTTN = x.ChuaQuaTTTN,
        LanLamLai = x.LanLamLai,
        HoSoTruocId = x.HoSoTruocId,
        CoDeTai = x.DeTaiId != null,
        SoQuyetDinhGiao = x.QuyetDinhGiao != null ? x.QuyetDinhGiao.SoQuyetDinh : null,
        RowVersion = x.RowVersion,
        CoKhieuNaiChoXuLy = db.DonTu.Any(d => d.SVTrongDotId == x.Id && d.LoaiDon == LoaiDon.KhieuNaiXetGiao
            && d.TrangThai == TrangThaiDonTu.ChoXuLy),
        MoiNhat = db.XetDieuKien
            .Where(k => k.SVTrongDotId == x.Id && (k.LoaiXet == LoaiXet.Giao || k.LoaiXet == LoaiXet.XetLai))
            .OrderByDescending(k => k.ThoiDiemXet).ThenByDescending(k => k.Id)
            .Select(k => new SnapshotTomTat
            {
                KetQua = k.KetQua,
                MaNgoaiLe = k.MaNgoaiLe,
                RuleVersion = k.RuleVersion,
                ThoiDiemXet = k.ThoiDiemXet,
                TCChuaTichLuy = k.TCChuaTichLuy,
            }).FirstOrDefault(),
    });

    private static HoSoXetGiaoItem ToHoSoItem(HoSoRow x) => new(x.Id, x.SinhVienId, x.MaSV, x.HoTen, x.KhoaHoc, x.MaCTDT, x.TenCTDT,
        x.TrangThai, x.CoNoTinChi, x.ChuaQuaTTTN, x.LanLamLai, x.HoSoTruocId, x.CoDeTai, x.SoQuyetDinhGiao,
        x.MoiNhat?.TCChuaTichLuy, x.MoiNhat?.KetQua, x.MoiNhat?.MaNgoaiLe, x.MoiNhat?.RuleVersion, x.MoiNhat?.ThoiDiemXet,
        x.CoKhieuNaiChoXuLy, Convert.ToBase64String(x.RowVersion));

    private static IQueryable<KhieuNaiItem> KhieuNaiQuery(IQueryable<DonTu> query) => query
        .Where(x => x.LoaiDon == LoaiDon.KhieuNaiXetGiao)
        .OrderByDescending(x => x.NgayGui).ThenByDescending(x => x.Id)
        .Select(x => new KhieuNaiItem(x.Id, x.SVTrongDotId, x.SVTrongDot.SinhVien.MaSV, x.SVTrongDot.SinhVien.HoTen,
            x.NoiDung, x.NgayGui, x.TrangThai, x.KetQuaXuLy, x.NgayXuLy));

    private long SinhVienHienTai() => currentUser.SinhVienId
        ?? throw Loi("UPM_CHI_SINH_VIEN", "Chức năng này chỉ dành cho tài khoản sinh viên.");

    private async Task KiemTraChuaCoQuyetDinhAsync(long id, CancellationToken ct)
    {
        var query = await scope.LocAsync(db.SVTrongDot.AsNoTracking(), ct: ct);
        var record = await query.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw Loi("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ trong phạm vi được phép.");
        // A19: "không tự lật khi QĐ giao đã ký".
        if (record.QuyetDinhGiaoId is not null || await db.QuyetDinhDoiTuong.AnyAsync(x => x.SVTrongDotId == id
            && x.QuyetDinh.LoaiQD == LoaiQuyetDinh.GiaoDATN, ct))
            throw Loi("UPM_A19_DA_CO_QUYET_DINH_GIAO", "Không thể xét lại vì hồ sơ đã có quyết định giao (dữ liệu đã khóa).");
    }

    private async Task<DotDATN> LayDotBatBuocAsync(long id, CancellationToken ct)
    {
        var query = await scope.LocAsync(db.DotDATN.AsQueryable(), ct: ct);
        return await query.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw Loi("UPM_DOT_NOT_FOUND", "Không tìm thấy đợt trong phạm vi được phép.");
    }

    private async Task<DotDATN> LayDotMoAsync(long id, CancellationToken ct)
    {
        var dot = await LayDotBatBuocAsync(id, ct);
        if (dot.TrangThai == TrangThaiDot.Dong) throw Loi("UPM_DOT_DA_DONG", "Đợt đã đóng, không thêm sinh viên.");
        return dot;
    }

    private async Task KiemTraQuyenAsync(string permission, CancellationToken ct)
    {
        var current = await quyen.LayAsync(ct);
        if (!current.CoQuyen(permission)) throw Loi("UPM_PERMISSION_DENIED", "Bạn không có quyền thực hiện thao tác này.");
    }

    private static void KiemTraDot(DotDATNRequest r)
    {
        ArgumentNullException.ThrowIfNull(r);
        var namHoc = r.NamHoc?.Trim() ?? string.Empty;
        if (r.CoSoId <= 0 || string.IsNullOrWhiteSpace(r.MaDot) || r.MaDot.Trim().Length > 20
            || string.IsNullOrWhiteSpace(r.TenDot) || r.TenDot.Trim().Length > 150 || namHoc.Length != 9
            || namHoc[4] != '-' || !int.TryParse(namHoc.AsSpan(0, 4), out var fromYear)
            || !int.TryParse(namHoc.AsSpan(5, 4), out var toYear) || toYear != fromYear + 1
            || r.HocKy is < 1 or > 3 || r.SoTuan is < 12 or > 13 || r.NgayNopQuyenHan < r.NgayBatDau)
            throw Loi("UPM_DOT_THONG_TIN_KHONG_HOP_LE",
                "Thông tin đợt không hợp lệ (năm học YYYY-YYYY, học kỳ 1–3, thời lượng 12–13 tuần, hạn nộp quyển không trước ngày bắt đầu).");
    }

    /// <summary>Mốc tiến độ mặc định của một đợt (đặc tả mục 5): tuần 3–4, 8–9, 12–13.</summary>
    internal static IEnumerable<MocTienDo> SinhMocMacDinh(DotDATN dot) =>
        new[] { MaMocTienDo.M1, MaMocTienDo.M2, MaMocTienDo.M3 }.Select(moc =>
        {
            var (tu, den) = TienDoRules.TuanMoc(moc);
            return new MocTienDo
            {
                DotId = dot.Id,
                MaMoc = moc,
                TuanTu = tu,
                TuanDen = den,
                NgayHan = TienDoRules.HanMacDinh(moc, dot.NgayBatDau, dot.NgayNopQuyenHan),
                YeuCauToiThieuPct = moc == MaMocTienDo.M2 ? TienDoRules.YeuCauM2MacDinh : null,
                MoTa = moc switch
                {
                    MaMocTienDo.M1 => "Duyệt đề cương / Chương 1",
                    MaMocTienDo.M2 => "Tiến độ 60–70%",
                    _ => "Duyệt 100% và nộp quyển",
                },
            };
        });

    private static DotDATNItem ToItem(DotDATN x, int soHoSo) =>
        new(x.Id, x.CoSoId, x.MaDot, x.TenDot, x.NamHoc, x.HocKy, x.NgayBatDau, x.SoTuan, x.NgayNopQuyenHan, x.TrangThai, soHoSo);

    private static DomainException Loi(string code, string message) => new(code, message);

    private sealed class HoSoRow
    {
        public long Id { get; init; }
        public long SinhVienId { get; init; }
        public string MaSV { get; init; } = string.Empty;
        public string HoTen { get; init; } = string.Empty;
        public int KhoaHoc { get; init; }
        public string MaCTDT { get; init; } = string.Empty;
        public string TenCTDT { get; init; } = string.Empty;
        public TrangThaiSVTrongDot TrangThai { get; init; }
        public bool CoNoTinChi { get; init; }
        public bool ChuaQuaTTTN { get; init; }
        public int LanLamLai { get; init; }
        public long? HoSoTruocId { get; init; }
        public bool CoDeTai { get; init; }
        public string? SoQuyetDinhGiao { get; init; }
        public byte[] RowVersion { get; init; } = [];
        public bool CoKhieuNaiChoXuLy { get; init; }
        public SnapshotTomTat? MoiNhat { get; init; }
    }

    private sealed class SnapshotTomTat
    {
        public string KetQua { get; init; } = string.Empty;
        public string? MaNgoaiLe { get; init; }
        public string RuleVersion { get; init; } = string.Empty;
        public DateTime ThoiDiemXet { get; init; }
        public int TCChuaTichLuy { get; init; }
    }
}
