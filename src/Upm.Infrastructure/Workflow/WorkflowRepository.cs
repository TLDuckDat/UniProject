using Microsoft.EntityFrameworkCore;
using Upm.Application.Abstractions;
using Upm.Application.PhanQuyen;
using Upm.Application.Workflow;
using Upm.Domain.Common;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Domain.Rules;
using Upm.Domain.PhanQuyen;
using Upm.Domain.Workflow;
using Upm.Infrastructure.Identity;
using Upm.Infrastructure.Persistence;

namespace Upm.Infrastructure.Workflow;

/// <summary>EF adapter cho use case workflow; mọi lần lưu trạng thái + lịch sử dùng cùng transaction.</summary>
public sealed class WorkflowRepository(
    UpmDbContext db,
    IScopeFilter scopeFilter,
    IQuyenHienTai quyen,
    ICurrentUser currentUser,
    IClock clock) : IWorkflowRepository
{
    public async Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is not null)
        {
            return await action(ct);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await action(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<SVTrongDot?> LayHoSoAsync(long id, bool heThong, CancellationToken ct)
    {
        if (heThong)
        {
            return await db.SVTrongDot.SingleOrDefaultAsync(x => x.Id == id, ct);
        }

        var scoped = await scopeFilter.LocAsync(db.SVTrongDot.AsQueryable(), ct: ct);
        var hoSo = await scoped.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (hoSo is not null) return hoSo;

        // Phân quyền theo từng SV (CLAUDE.md §5.7): GV đang là GVHD/GVĐD/đồng HD của hồ sơ thì được thao tác,
        // dù GV thuộc khoa khác (phạm vi khoa không bao hồ sơ).
        if (currentUser.GiangVienId is { } gvId)
        {
            var laGv = await db.SVTrongDot.SingleOrDefaultAsync(x => x.Id == id
                && x.PhanCongs.Any(p => p.GiangVienId == gvId && p.NgayKetThuc == null), ct);
            if (laGv is not null) return laGv;
        }

        // Trưởng Bộ môn: vai trò gắn bộ môn không mang khoa trong phạm vi (PhamViTruyCap), nên thấy hồ sơ
        // thuộc khoa của các bộ môn mình quản lý (đồng bộ với phạm vi phân công / đơn tiến độ).
        var pv = (await quyen.LayAsync(ct)).PhamVi([VaiTroMa.TruongBoMon]);
        if (pv.ToanHeThong || pv.BoMonIds.Count == 0) return null;
        var boMonIds = pv.BoMonIds.ToArray();
        var khoaIds = await db.BoMon.AsNoTracking().Where(b => boMonIds.Contains(b.Id)).Select(b => b.KhoaId).ToArrayAsync(ct);
        return await db.SVTrongDot.SingleOrDefaultAsync(x => x.Id == id && khoaIds.Contains(x.CTDT.KhoaId), ct);
    }

    public async Task<WorkflowEvidence> LayBangChungAsync(SVTrongDot hoSo, CancellationToken ct)
    {
        var facts = new HashSet<WorkflowFact>();

        var dot = await db.DotDATN.Where(x => x.Id == hoSo.DotId)
            .Select(x => new { x.NgayBatDau, x.NgayNopQuyenHan })
            .SingleAsync(ct);
        if (clock.Today >= dot.NgayBatDau)
        {
            facts.Add(WorkflowFact.DenNgayBatDauDot);
        }

        var trangThaiHocTap = await db.SinhVien.Where(x => x.Id == hoSo.SinhVienId)
            .Select(x => x.TrangThaiHocTap)
            .SingleAsync(ct);
        if (trangThaiHocTap == TrangThaiHocTap.NghiTamThoi)
        {
            facts.Add(WorkflowFact.SinhVienNghiTamThoi);
        }

        // Kết quả xét giao có hiệu lực = snapshot Giao/XetLai mới nhất (A19).
        var giao = await db.XetDieuKien.Where(x => x.SVTrongDotId == hoSo.Id
                && (x.LoaiXet == LoaiXet.Giao || x.LoaiXet == LoaiXet.XetLai))
            .OrderByDescending(x => x.ThoiDiemXet).ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
        if (giao?.KetQua == nameof(KetLuanXetGiao.KHONG_DU_DK))
        {
            facts.Add(WorkflowFact.GiaoKhongDuDieuKien);
        }
        else if (giao?.KetQua == nameof(KetLuanXetGiao.KHONG_DU_DK_CHUYEN_THAY_THE))
        {
            facts.Add(WorkflowFact.GiaoChuyenHocPhanThayThe);
        }
        else if (giao?.KetQua == nameof(KetLuanXetGiao.DU_DK))
        {
            facts.Add(WorkflowFact.GiaoDuDieuKien);
        }
        if (hoSo.DeTaiId is not null)
        {
            facts.Add(WorkflowFact.CoDeTai);
        }
        if (hoSo.QuyetDinhGiaoId is not null)
        {
            facts.Add(WorkflowFact.CoQuyetDinhGiao);
        }

        var requests = await db.DonTu.Where(x => x.SVTrongDotId == hoSo.Id && x.TrangThai == TrangThaiDonTu.DaDuyet)
            .Select(x => x.LoaiDon)
            .ToListAsync(ct);
        if (requests.Contains(LoaiDon.XinDung))
        {
            facts.Add(WorkflowFact.DonXinDungDaDuyet);
        }
        if (requests.Contains(LoaiDon.GiaHan))
        {
            facts.Add(WorkflowFact.DonGiaHanDaDuyet);
        }

        var quyenNop = await db.PhienBanQuyen.Where(x => x.SVTrongDotId == hoSo.Id && x.LoaiBan == LoaiBanQuyen.NopQuyen)
            .OrderByDescending(x => x.SoPhienBan)
            .FirstOrDefaultAsync(ct);
        if (quyenNop is not null)
        {
            facts.Add(WorkflowFact.CoBanQuyenNop);
            if (DateOnly.FromDateTime(quyenNop.NgayNop) <= dot.NgayNopQuyenHan)
            {
                facts.Add(WorkflowFact.CoBanQuyenNopDungHan);
            }
        }

        var suaGap = await db.PhienBanQuyen.Where(x => x.SVTrongDotId == hoSo.Id && x.LoaiBan == LoaiBanQuyen.BanSuaGap)
            .OrderByDescending(x => x.SoPhienBan)
            .FirstOrDefaultAsync(ct);
        if (suaGap is not null)
        {
            facts.Add(WorkflowFact.CoBanQuyenSuaGap);
        }

        var docDuyet = await db.KetQuaDocDuyet.Where(x => x.SVTrongDotId == hoSo.Id)
            .OrderByDescending(x => x.NgayQuyet)
            .FirstOrDefaultAsync(ct);
        if (docDuyet?.KetQua == KetQuaDocDuyetLoai.SuaGap)
        {
            facts.Add(WorkflowFact.KetQuaDocDuyetSuaGap);
        }
        if (docDuyet?.KetQua == KetQuaDocDuyetLoai.KhongDongY)
        {
            facts.Add(WorkflowFact.GVDDKhongDongY);
        }
        if (docDuyet is not null && docDuyet.VongDoc <= XetBaoVe.SoVongSuaGapToiDa)
        {
            facts.Add(WorkflowFact.ConVongSuaGap);
        }
        if (suaGap is not null && docDuyet is not null)
        {
            // PhienBanQuyenId là khóa, không phải số phiên bản; xác nhận theo FK của bản được đọc.
            var reviewedVersion = await db.PhienBanQuyen.Where(x => x.Id == docDuyet.PhienBanQuyenId)
                .Select(x => x.SoPhienBan).SingleAsync(ct);
            if (suaGap.SoPhienBan > reviewedVersion)
            {
                facts.Add(WorkflowFact.BanSuaGapMoiNhat);
            }
        }
        else if (suaGap is not null && docDuyet is null)
        {
            facts.Add(WorkflowFact.BanSuaGapMoiNhat);
        }

        var yKienGVHD = await db.YKienGVHD.Where(x => x.SVTrongDotId == hoSo.Id)
            .OrderByDescending(x => x.NgayCho)
            .FirstOrDefaultAsync(ct);
        if (yKienGVHD is { DongY: false })
        {
            facts.Add(WorkflowFact.GVHDKhongDongY);
        }

        var baoVe = await db.XetDieuKien.Where(x => x.SVTrongDotId == hoSo.Id && x.LoaiXet == LoaiXet.BaoVe)
            .OrderByDescending(x => x.ThoiDiemXet)
            .FirstOrDefaultAsync(ct);
        var xetLai = await db.XetDieuKien.Where(x => x.SVTrongDotId == hoSo.Id && x.LoaiXet == LoaiXet.XetLai)
            .OrderByDescending(x => x.ThoiDiemXet)
            .FirstOrDefaultAsync(ct);

        if (hoSo.NgayHanHoanBaoVe is { } deadline && clock.Today > deadline)
        {
            facts.Add(WorkflowFact.HetHanHoanBaoVe);
        }

        var lichBaoVe = await db.LichBaoVe.Where(x => x.SVTrongDotId == hoSo.Id)
            .OrderByDescending(x => x.NgayBaoVe).ThenByDescending(x => x.Ca)
            .FirstOrDefaultAsync(ct);
        if (lichBaoVe?.TrangThai == TrangThaiLichBaoVe.VangCoLyDo)
        {
            facts.Add(WorkflowFact.VangBaoVeCoLyDo);
        }
        if (lichBaoVe?.TrangThai == TrangThaiLichBaoVe.VangKhongLyDo)
        {
            facts.Add(WorkflowFact.DiemDATNKhongDat);
        }
        if (lichBaoVe?.TrangThai is TrangThaiLichBaoVe.DaBaoVe or TrangThaiLichBaoVe.BaoVeBu)
        {
            var presentMemberIds = await db.ThanhVienHoiDong
                .Where(x => x.HoiDongId == lichBaoVe.HoiDongId && x.CoMat)
                .Select(x => x.Id)
                .ToListAsync(ct);
            if (presentMemberIds.Count > 0)
            {
                var submittedScores = await db.PhieuDiem
                    .CountAsync(x => x.LichBaoVeId == lichBaoVe.Id && x.DaNop && presentMemberIds.Contains(x.ThanhVienHoiDongId), ct);
                if (submittedScores == presentMemberIds.Count)
                {
                    facts.Add(WorkflowFact.BaoVeHoanTatDuPhieu);
                }
            }
        }

        var diem = await db.DiemDATN.Where(x => x.SVTrongDotId == hoSo.Id)
            .OrderByDescending(x => x.ChotLuc)
            .FirstOrDefaultAsync(ct);
        if (diem?.ChotLuc is not null)
        {
            facts.Add(diem.Dat ? WorkflowFact.DiemDATNDat : WorkflowFact.DiemDATNKhongDat);
        }

        var finalBook = await (from storage in db.LuuTruThuVien
                               join version in db.PhienBanQuyen on storage.PhienBanQuyenId equals version.Id
                               where version.SVTrongDotId == hoSo.Id && version.LoaiBan == LoaiBanQuyen.BanCuoi
                               orderby storage.NgayNhan descending
                               select new { storage.GVHDXacNhan }).FirstOrDefaultAsync(ct);
        if (finalBook is not null)
        {
            facts.Add(WorkflowFact.CoBanCuoiDaNhan);
            facts.Add(WorkflowFact.ThuVienNhanBanCuoi);
            if (finalBook.GVHDXacNhan)
            {
                facts.Add(WorkflowFact.GVHDXacNhanBanCuoi);
            }
        }

        var snapshot = hoSo.TrangThai == TrangThaiSVTrongDot.HOAN_BAO_VE ? xetLai : baoVe;
        return new WorkflowEvidence(facts, KetQua(snapshot));
    }

    public async Task<IReadOnlyList<LichSuTrangThai>> LayLichSuAsync(long id, CancellationToken ct)
    {
        var scoped = await scopeFilter.LocAsync(db.SVTrongDot.AsQueryable(), ct: ct);
        var accessible = await scoped.AnyAsync(x => x.Id == id, ct);
        if (!accessible)
        {
            throw new DomainException("UPM_WORKFLOW_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ hoặc bạn không có quyền truy cập.");
        }

        return await db.LichSuTrangThai.Where(x => x.SVTrongDotId == id)
            .OrderBy(x => x.ThoiDiem).ThenBy(x => x.Id)
            .ToArrayAsync(ct);
    }

    public async Task LuuChuyenTrangThaiAsync(SVTrongDot hoSo, LichSuTrangThai lichSu, byte[] rowVersionDuKien, CancellationToken ct)
    {
        db.Entry(hoSo).Property(x => x.RowVersion).OriginalValue = rowVersionDuKien;
        db.LichSuTrangThai.Add(lichSu);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException("UPM_WORKFLOW_CONCURRENCY_CONFLICT", "Hồ sơ đã được cập nhật bởi thao tác khác. Hãy tải lại trạng thái mới nhất.");
        }
    }

    public Task<DotDATN?> LayDotAsync(long id, CancellationToken ct) => db.DotDATN.SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task<string?> LaySoQuyetDinhAsync(long id, CancellationToken ct) =>
        db.QuyetDinh.Where(x => x.Id == id).Select(x => x.SoQuyetDinh).SingleOrDefaultAsync(ct);

    public Task<bool> DaCoHoSoAsync(long dotId, long sinhVienId, long ctdtId, CancellationToken ct) =>
        db.SVTrongDot.AnyAsync(x => x.DotId == dotId && x.SinhVienId == sinhVienId && x.CTDTId == ctdtId, ct);

    public async Task LuuHoSoMoiAsync(SVTrongDot hoSo, LichSuTrangThai lichSu, CancellationToken ct)
    {
        db.SVTrongDot.Add(hoSo);
        lichSu.SVTrongDot = hoSo;
        db.LichSuTrangThai.Add(lichSu);
        await db.SaveChangesAsync(ct);
    }

    private static List<IKetQuaLuat> KetQua(XetDieuKien? snapshot)
    {
        if (snapshot is null)
        {
            return [];
        }

        var results = new List<IKetQuaLuat>();
        Them(results, MaLuat.K1, snapshot.K1_KyLuat);
        Them(results, MaLuat.K2, snapshot.K2_GVHD);
        Them(results, MaLuat.K3, snapshot.K3_GVDD);
        Them(results, MaLuat.K4, snapshot.K4_DuTC);
        Them(results, MaLuat.K5, snapshot.K5_Gpa);
        Them(results, MaLuat.K6, snapshot.K6_TTTN);
        return results;
    }

    private static void Them(List<IKetQuaLuat> results, string maLuat, bool? dat)
    {
        if (dat is { } value)
        {
            results.Add(value
                ? KetQuaLuat.Qua(maLuat, $"{maLuat} đạt theo snapshot xét điều kiện.")
                : KetQuaLuat.KhongQua(maLuat, $"{maLuat} không đạt theo snapshot xét điều kiện."));
        }
    }
}
