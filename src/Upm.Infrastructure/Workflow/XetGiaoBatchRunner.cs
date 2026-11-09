using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upm.Application.Abstractions;
using Upm.Application.Workflow;
using Upm.Domain.Common;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Domain.Rules;
using Upm.Infrastructure.Persistence;

namespace Upm.Infrastructure.Workflow;

/// <summary>
/// UC-1 "Xét điều kiện giao ĐATN" (đặc tả mục 6, A01–A21): nạp dữ liệu → <see cref="XetGiao.Xet"/> → snapshot
/// <c>XetDieuKien</c> + <c>XetDieuKienChiTiet</c> → chuyển trạng thái qua <see cref="IWorkflowService"/> (T01/T02, A19: T01R/T02R).
/// Mỗi hồ sơ một transaction + applock theo hồ sơ → chạy lại / chạy song song không nhân đôi snapshot.
/// </summary>
public sealed class XetGiaoBatchRunner(
    UpmDbContext db,
    IWorkflowService workflow,
    IClock clock,
    ILogger<XetGiaoBatchRunner> logger) : IXetGiaoBatchRunner
{
    /// <summary>Số hồ sơ mỗi lô: sau mỗi lô dọn ChangeTracker để job dài không phình bộ nhớ.</summary>
    internal const int KichThuocLo = 50;

    private const int SoLoiGhiLaiToiDa = 200;

    private readonly Dictionary<long, IReadOnlyList<HocPhanCTDT>> _hocPhanTheoCTDT = [];
    private List<AnhXaRuleSetKhoa>? _anhXaRuleSet;
    private Dictionary<string, RuleSet>? _ruleSetTheoMa;

    public async Task<XetGiaoTienDo> ChayAsync(
        long dotId, long? nguoiChayId, IProgress<XetGiaoTienDo>? progress = null, CancellationToken ct = default)
    {
        var dot = await db.DotDATN.AsNoTracking().SingleOrDefaultAsync(x => x.Id == dotId, ct)
            ?? throw new DomainException("UPM_DOT_NOT_FOUND", "Không tìm thấy đợt để chạy xét.");
        if (dot.TrangThai == TrangThaiDot.Dong)
            throw new DomainException("UPM_DOT_DA_DONG", "Đợt đã đóng, không thể chạy xét.");

        // Idempotent: chỉ lấy hồ sơ CHO_XET CHƯA có snapshot xét giao. Hồ sơ đủ ĐK (vẫn CHO_XET chờ đề tài + QĐ) không bị xét lại.
        var ids = await db.SVTrongDot.AsNoTracking()
            .Where(x => x.DotId == dotId && x.TrangThai == TrangThaiSVTrongDot.CHO_XET
                && !db.XetDieuKien.Any(k => k.SVTrongDotId == x.Id && (k.LoaiXet == LoaiXet.Giao || k.LoaiXet == LoaiXet.XetLai)))
            .OrderBy(x => x.Id).Select(x => x.Id).ToListAsync(ct);

        var loi = new List<XetGiaoLoiItem>();
        var daXuLy = 0;
        progress?.Report(new XetGiaoTienDo(0, ids.Count, loi.ToArray()));
        foreach (var lo in ids.Chunk(KichThuocLo))
        {
            foreach (var id in lo)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await XetMotHoSoAsync(id, LoaiXet.Giao, nguoiChayId, laJob: true, ct);
                }
                catch (DomainException ex)
                {
                    // Lỗi dữ liệu của MỘT SV (thiếu tiến độ, RuleSet…) không dừng cả đợt; chạy lại sau khi bổ sung dữ liệu.
                    var maSV = await db.SVTrongDot.Where(x => x.Id == id).Select(x => x.SinhVien.MaSV).SingleAsync(ct);
                    logger.LogWarning("Xét giao hồ sơ {SVTrongDotId} ({MaSV}) lỗi {Code}: {Message}", id, maSV, ex.Code, ex.Message);
                    if (loi.Count < SoLoiGhiLaiToiDa) loi.Add(new XetGiaoLoiItem(id, maSV, ex.Code, ex.Message));
                }
                daXuLy++;
                progress?.Report(new XetGiaoTienDo(daXuLy, ids.Count, loi.ToArray()));
            }
            db.ChangeTracker.Clear();
        }
        logger.LogInformation("Xét giao đợt {DotId}: {DaXuLy}/{Tong} hồ sơ, {SoLoi} lỗi.", dotId, daXuLy, ids.Count, loi.Count);
        return new XetGiaoTienDo(daXuLy, ids.Count, loi);
    }

    public Task<XetGiaoSnapshotItem> XetLaiAsync(long svTrongDotId, long? nguoiChayId, CancellationToken ct = default) =>
        XetMotHoSoAsync(svTrongDotId, LoaiXet.XetLai, nguoiChayId, laJob: false, ct);

    private async Task<XetGiaoSnapshotItem> XetMotHoSoAsync(
        long svTrongDotId, LoaiXet loaiXet, long? nguoiChayId, bool laJob, CancellationToken ct)
    {
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction ? await db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            await KhoaHoSoAsync(svTrongDotId, ct);
            var hoSo = await db.SVTrongDot.Include(x => x.SinhVien).Include(x => x.CTDT)
                .SingleOrDefaultAsync(x => x.Id == svTrongDotId, ct)
                ?? throw new DomainException("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ sinh viên trong đợt.");

            if (laJob)
            {
                // Kiểm tra lại SAU khi giữ khóa: job khác có thể vừa xét xong hồ sơ này.
                var daCo = await db.XetDieuKien.Where(x => x.SVTrongDotId == svTrongDotId
                        && (x.LoaiXet == LoaiXet.Giao || x.LoaiXet == LoaiXet.XetLai))
                    .OrderByDescending(x => x.ThoiDiemXet).ThenByDescending(x => x.Id)
                    .Select(x => (long?)x.Id).FirstOrDefaultAsync(ct);
                if (daCo is not null || hoSo.TrangThai != TrangThaiSVTrongDot.CHO_XET)
                {
                    if (ownsTransaction) await transaction!.CommitAsync(ct);
                    return daCo is null
                        ? throw new DomainException("UPM_XET_GIAO_TRANG_THAI_KHONG_HOP_LE", "Chỉ hồ sơ CHO_XET được chạy xét giao.")
                        : await DocSnapshotAsync(db, daCo.Value, ct);
                }
            }
            else if (hoSo.TrangThai is not (TrangThaiSVTrongDot.CHO_XET or TrangThaiSVTrongDot.KHONG_DU_DK
                     or TrangThaiSVTrongDot.KHONG_DU_DK_CHUYEN_THAY_THE))
            {
                throw new DomainException("UPM_A19_TRANG_THAI_KHONG_XET_LAI",
                    $"Hồ sơ đang ở trạng thái {hoSo.TrangThai}; chỉ xét lại hồ sơ chờ xét hoặc không đủ điều kiện, chưa có quyết định giao.");
            }

            var dot = await db.DotDATN.AsNoTracking().SingleAsync(x => x.Id == hoSo.DotId, ct);
            var ruleSet = await ChonRuleSetAsync(hoSo.SinhVien.KhoaHoc, ct);
            var tienDo = await db.ThongTinTienDoSV.AsNoTracking().SingleOrDefaultAsync(x => x.SinhVienId == hoSo.SinhVienId, ct)
                ?? throw new DomainException("UPM_XET_GIAO_THIEU_TIEN_DO",
                    "Chưa đồng bộ thông tin tiến độ (kỳ cuối kế hoạch, hạn học tối đa) của sinh viên.");
            var kyXet = KyXetCuaDot(dot);

            var bangDiem = await db.BangDiem.AsNoTracking().Where(x => x.SinhVienId == hoSo.SinhVienId)
                .Select(x => new DiemHocPhan(x.HocPhanId, x.TrangThaiDiem, x.LanHoc)).ToListAsync(ct);
            var kyLuat = (await db.KyLuatSV.AsNoTracking().Where(x => x.SinhVienId == hoSo.SinhVienId)
                    .Select(x => new { x.MucKyLuat, x.TuNgay, x.DenNgay, x.DinhChiHocTap, x.TruyCuuHinhSu }).ToListAsync(ct))
                .Select(x => new KyLuat(
                    // Nợ kỹ thuật Phần 4: chuỗi mức kỷ luật từ CTSV chưa có bảng ánh xạ; G1 chỉ dựa vào cờ đình chỉ / hình sự.
                    Enum.TryParse<MucKyLuat>(x.MucKyLuat, true, out var muc) ? muc : MucKyLuat.KhienTrach,
                    x.TuNgay, x.DenNgay, x.DinhChiHocTap, x.TruyCuuHinhSu))
                .ToArray();

            var input = new HoSoXetGiao
            {
                TrangThaiHocTap = hoSo.SinhVien.TrangThaiHocTap,
                KyLuat = kyLuat,
                HanToiDa = tienDo.HanToiDa,
                HocTap = new DuLieuHocTapSV
                {
                    HocPhanCTDT = await HocPhanCTDTAsync(hoSo.CTDTId, ct),
                    BangDiem = bangDiem,
                    KyCuoiKeHoach = HocKy.Parse(tienDo.KyCuoiKeHoach),
                },
                CTDTCoHocPhanThayThe = hoSo.CTDT.CoHocPhanThayThe,
            };
            var ketQua = XetGiao.Xet(input, ThamSoQuyChe.Tu(ruleSet), kyXet, clock.Today);

            hoSo.CoNoTinChi = ketQua.Co.Contains(CoXetGiao.NO_TIN_CHI);
            hoSo.ChuaQuaTTTN = !ketQua.TinChi.DaDatTTTN;

            var snapshot = TaoSnapshot(hoSo.Id, loaiXet, ruleSet, ketQua, nguoiChayId);
            db.XetDieuKien.Add(snapshot);
            await db.SaveChangesAsync(ct);

            await DongBoTrangThaiAsync(hoSo, ketQua, laJob, ct);
            if (ownsTransaction) await transaction!.CommitAsync(ct);
            return await DocSnapshotAsync(db, snapshot.Id, ct);
        }
        catch
        {
            if (ownsTransaction && transaction is not null) await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// Trạng thái đích theo kết luận: DU_DK → CHO_XET (chờ đề tài + QĐ để T03), KHONG_DU_DK → T01, chuyển thay thế → T02.
    /// A19: hồ sơ đang KHONG_DU_DK* mà xét lại đủ điều kiện → T01R/T02R về CHO_XET.
    /// </summary>
    private async Task DongBoTrangThaiAsync(SVTrongDot hoSo, KetQuaXetGiao ketQua, bool laJob, CancellationToken ct)
    {
        var maChuyen = (hoSo.TrangThai, ketQua.KetLuan) switch
        {
            (TrangThaiSVTrongDot.CHO_XET, KetLuanXetGiao.KHONG_DU_DK) => "T01",
            (TrangThaiSVTrongDot.CHO_XET, KetLuanXetGiao.KHONG_DU_DK_CHUYEN_THAY_THE) => "T02",
            (TrangThaiSVTrongDot.KHONG_DU_DK, KetLuanXetGiao.DU_DK) => "T01R",
            (TrangThaiSVTrongDot.KHONG_DU_DK_CHUYEN_THAY_THE, KetLuanXetGiao.DU_DK) => "T02R",
            // Cùng trạng thái, hoặc đổi giữa hai loại "không đủ" (chỉ khi CTĐT đổi cờ HP thay thế) → giữ nguyên, snapshot đã ghi kết quả mới.
            _ => null,
        };
        if (maChuyen is null) return;

        var request = new WorkflowTransitionRequest(Convert.ToBase64String(hoSo.RowVersion), MaChuyen: maChuyen,
            LyDo: Cat(laJob ? ketQua.LyDo : $"Xét lại (A19): {ketQua.LyDo}", 500));
        _ = laJob
            ? await workflow.TransitionHeThongAsync(hoSo.Id, request, ct)
            : await workflow.TransitionAsync(hoSo.Id, request, ct);
    }

    private XetDieuKien TaoSnapshot(long svTrongDotId, LoaiXet loaiXet, RuleSet ruleSet, KetQuaXetGiao ketQua, long? nguoiChayId)
    {
        var luat = ketQua.CacLuat.ToDictionary(x => x.MaLuat);
        var snapshot = new XetDieuKien
        {
            SVTrongDotId = svTrongDotId,
            LoaiXet = loaiXet,
            RuleSetId = ruleSet.Id,
            RuleVersion = ketQua.RuleVersion,
            ThoiDiemXet = clock.Now.UtcDateTime,
            NguoiChayId = nguoiChayId,
            KetQua = ketQua.KetLuan.ToString(),
            MaNgoaiLe = ketQua.MaNgoaiLe,
            TCChuaTichLuy = ketQua.TinChi.TongTC,
            // Xét giao không dùng GPA (mục 6); cột NOT NULL theo thiết kế → 0. GPA thật lưu ở snapshot xét bảo vệ.
            GpaTichLuy = 0m,
            // Với LoaiXet Giao/XetLai: K1 = G1 (kỷ luật/hình sự), K2 = G2 (trạng thái học tập, thời gian tối đa),
            // K4 = G3 (TC chưa tích lũy ≤ ngưỡng), K6 = đã đạt TTTN. K3, K5 không áp dụng.
            K1_KyLuat = luat[MaLuat.G1].Dat,
            K2_GVHD = luat[MaLuat.G2].Dat,
            K3_GVDD = null,
            K4_DuTC = ketQua.TinChi.Dat,
            K5_Gpa = null,
            K6_TTTN = ketQua.TinChi.DaDatTTTN,
            LyDo = Cat(ketQua.LyDo, 500),
        };
        foreach (var chiTiet in ketQua.TinChi.ChiTiet)
        {
            snapshot.ChiTiets.Add(new XetDieuKienChiTiet
            {
                HocPhanId = chiTiet.HocPhanId,
                SoTC = chiTiet.SoTC,
                LoaiNo = chiTiet.LoaiNo,
                BiLoaiTru = chiTiet.BiLoaiTru,
                MaNgoaiLe = chiTiet.MaNgoaiLe,
                LyDo = Cat(chiTiet.LyDo, 300),
            });
        }
        return snapshot;
    }

    /// <summary>Kỳ xét = năm học bắt đầu của đợt + học kỳ của đợt ("2026-2027", HK1 → 2026.1).</summary>
    internal static HocKy KyXetCuaDot(DotDATN dot) =>
        new(int.Parse(dot.NamHoc.AsSpan(0, 4), CultureInfo.InvariantCulture), dot.HocKy);

    /// <summary>A15/A16: RuleSet theo KHÓA sinh viên (không theo đợt) – dùng ChonRuleSet của rule engine.</summary>
    private async Task<RuleSet> ChonRuleSetAsync(int khoaHoc, CancellationToken ct)
    {
        if (_anhXaRuleSet is null || _ruleSetTheoMa is null)
        {
            var rows = await db.RuleSetKhoaHoc.AsNoTracking()
                .Select(x => new { x.RuleSet.MaRuleSet, x.KhoaHocTu, x.KhoaHocDen, x.CanXacNhan }).ToListAsync(ct);
            _anhXaRuleSet = rows.Select(x => new AnhXaRuleSetKhoa(x.MaRuleSet, x.KhoaHocTu, x.KhoaHocDen, x.CanXacNhan)).ToList();
            _ruleSetTheoMa = await db.RuleSet.AsNoTracking().ToDictionaryAsync(x => x.MaRuleSet, StringComparer.Ordinal, ct);
        }
        var chon = ChonRuleSet.TheoKhoa(khoaHoc, _anhXaRuleSet);
        return _ruleSetTheoMa[chon.MaRuleSet];
    }

    private async Task<IReadOnlyList<HocPhanCTDT>> HocPhanCTDTAsync(long ctdtId, CancellationToken ct)
    {
        if (_hocPhanTheoCTDT.TryGetValue(ctdtId, out var cached)) return cached;
        var rows = await db.CTDTHocPhan.AsNoTracking().Where(x => x.CTDTId == ctdtId)
            .Select(x => new HocPhanCTDT(x.HocPhan.Id, x.HocPhan.MaHP, x.HocPhan.SoTC, x.HocPhan.LoaiHP, x.HocPhan.TinhDiem,
                x.LaHPThayTheDATN))
            .ToListAsync(ct);
        _hocPhanTheoCTDT[ctdtId] = rows;
        return rows;
    }

    /// <summary>Khóa độc quyền theo hồ sơ trong transaction hiện tại (giải phóng khi commit/rollback).</summary>
    private async Task KhoaHoSoAsync(long svTrongDotId, CancellationToken ct)
    {
        var resource = $"upm:xet-giao:{svTrongDotId.ToString(CultureInfo.InvariantCulture)}";
        _ = await db.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @r int;
            EXEC @r = sp_getapplock @Resource = {resource}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000;
            IF @r < 0 THROW 51001, N'Không giữ được khóa xét giao của hồ sơ.', 1;
            """, ct);
    }

    internal static async Task<XetGiaoSnapshotItem> DocSnapshotAsync(UpmDbContext db, long id, CancellationToken ct) =>
        (await DocSnapshotsAsync(db, db.XetDieuKien.Where(x => x.Id == id), ct)).Single();

    /// <summary>Đọc snapshot (kèm chi tiết học phần, RuleSet, người chạy) theo thứ tự thời gian xét.</summary>
    internal static async Task<IReadOnlyList<XetGiaoSnapshotItem>> DocSnapshotsAsync(
        UpmDbContext db, IQueryable<XetDieuKien> query, CancellationToken ct)
    {
        var rows = await query.AsNoTracking()
            .OrderBy(x => x.ThoiDiemXet).ThenBy(x => x.Id)
            .Select(x => new
            {
                Snapshot = x,
                x.RuleSet.MaRuleSet,
                x.RuleSet.MaxTCChuaTichLuy,
                NguoiChay = x.NguoiChay == null ? null : x.NguoiChay.TenDangNhap,
                ChiTiets = x.ChiTiets.OrderBy(c => c.BiLoaiTru).ThenBy(c => c.HocPhan.MaHP).Select(c => new ChiTietXetGiaoItem(
                    c.HocPhan.MaHP, c.HocPhan.TenHP, c.HocPhan.LoaiHP, c.SoTC, c.LoaiNo, c.BiLoaiTru, c.MaNgoaiLe, c.LyDo)).ToList(),
            })
            .AsSplitQuery()
            .ToListAsync(ct);
        return rows.Select(r =>
        {
            var x = r.Snapshot;
            var co = new List<string>();
            if (x.KetQua == nameof(KetLuanXetGiao.DU_DK))
            {
                if (x.TCChuaTichLuy > 0) co.Add(nameof(CoXetGiao.NO_TIN_CHI));
                if (x.K6_TTTN == false) co.Add(nameof(CoXetGiao.CHUA_QUA_TTTN));
            }
            return new XetGiaoSnapshotItem(x.Id, x.LoaiXet, r.MaRuleSet, x.RuleVersion, x.ThoiDiemXet, r.NguoiChay, x.KetQua,
                x.MaNgoaiLe, x.TCChuaTichLuy, r.MaxTCChuaTichLuy, x.K1_KyLuat, x.K2_GVHD, x.K4_DuTC, x.K6_TTTN, co, x.LyDo, r.ChiTiets);
        }).ToArray();
    }

    private static string Cat(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";
}
