using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Upm.Application.Abstractions;
using Upm.Application.PhanQuyen;
using Upm.Application.QuyenDocDuyet;
using Upm.Application.TienDo;
using Upm.Application.Workflow;
using Upm.Domain.Common;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Domain.PhanQuyen;
using Upm.Domain.Rules;
using Upm.Domain.Workflow;
using Upm.Infrastructure.Persistence;
using Upm.Infrastructure.TienDo;

namespace Upm.Infrastructure.QuyenDocDuyet;

/// <summary>Cấu hình UC-3b. <c>SoNgayGVDDDoc</c>: hạn GVĐD đọc quyển (chờ BA xác nhận, xem PROGRESS).</summary>
public sealed class QuyenOptions
{
    public const string Section = "Quyen";
    public int SoNgayGVDDDoc { get; set; } = 7;
    /// <summary>Tự quét đạo văn khi nộp quyển (chỉ khi ThamSoDaoVan.Bat cho khoa × chương trình).</summary>
    public bool DaoVanTuDong { get; set; } = true;
}

/// <summary>
/// UC-3b: nộp quyển (T07 / T09), ý kiến GVHD (T12 khi không đồng ý), đọc duyệt GVĐD (T08 sửa gấp / T12),
/// quyển cần đọc, đạo văn (D11), đánh dấu bản bảo vệ và kiểm tra bảo vệ (D13), GVĐD trễ hạn và thay GVĐD (D09).
/// Luật thuần ở <see cref="QuyenRules"/>; máy trạng thái ở <see cref="IWorkflowService"/>.
/// </summary>
public sealed class QuyenService(
    UpmDbContext db,
    IQuyenHienTai quyen,
    ICurrentUser currentUser,
    IClock clock,
    IWorkflowService workflow,
    IFileStorage files,
    IOptions<TepOptions> tepOptions,
    IOptions<QuyenOptions> quyenOptions,
    IPlagiarismChecker checker) : IQuyenService
{
    private static readonly string[] DinhDangQuyenCho = [".pdf", ".docx"];

    // ------------------------------------------------------------------ Nộp quyển

    public async Task<PhienBanQuyenItem> NopQuyenAsync(long svTrongDotId, Stream file, string tenFile, string contentType, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        var sv = SinhVienHienTai();
        var hoSo = await db.SVTrongDot.Include(x => x.Dot).Include(x => x.CTDT)
            .SingleOrDefaultAsync(x => x.Id == svTrongDotId && x.SinhVienId == sv, ct)
            ?? throw Loi("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ của sinh viên hiện tại.");

        LoaiBanQuyen loai;
        if (hoSo.TrangThai == TrangThaiSVTrongDot.DANG_THUC_HIEN) loai = LoaiBanQuyen.NopQuyen;
        else if (hoSo.TrangThai == TrangThaiSVTrongDot.YEU_CAU_CHINH_SUA) loai = LoaiBanQuyen.BanSuaGap;
        else throw Loi("UPM_QUYEN_TRANG_THAI", "Chỉ nộp quyển khi đang thực hiện, hoặc nộp bản sửa gấp khi được yêu cầu.");

        var ext = Path.GetExtension(tenFile).ToLowerInvariant();
        if (!DinhDangQuyenCho.Contains(ext)) throw Loi("UPM_TEP_DINH_DANG", "Quyển chỉ nhận PDF hoặc DOCX.");

        var now = clock.Now.UtcDateTime;
        var hanGiaHan = await HanGiaHanAsync(hoSo.Id, ct);
        var kqHan = QuyenRules.KiemTraHanNopQuyen(clock.Today, hoSo.Dot.NgayNopQuyenHan, hanGiaHan);
        if (loai == LoaiBanQuyen.NopQuyen && !kqHan.Dat) throw Loi("UPM_D12_QUA_HAN_NOP", kqHan.LyDo);

        var gioiHan = (long)tepOptions.Value.KichThuocToiDaMB * 1024 * 1024;
        if (file.CanSeek && file.Length > gioiHan) throw Loi("UPM_TEP_QUA_DUNG_LUONG", $"File vượt {tepOptions.Value.KichThuocToiDaMB} MB.");
        var luu = await files.LuuAsync(file, tenFile, contentType, ct);
        if (luu.KichThuocByte > gioiHan) throw Loi("UPM_TEP_QUA_DUNG_LUONG", $"File vượt {tepOptions.Value.KichThuocToiDaMB} MB.");
        var tep = new TepDinhKem
        {
            TenGoc = Path.GetFileName(tenFile),
            ContentType = contentType,
            KichThuocByte = luu.KichThuocByte,
            DuongDan = luu.DuongDan,
            HashSHA256 = luu.HashSHA256,
        };
        db.TepDinhKem.Add(tep);
        await db.SaveChangesAsync(ct);

        var soPhienBan = (await db.PhienBanQuyen.Where(x => x.SVTrongDotId == hoSo.Id).Select(x => (int?)x.SoPhienBan).MaxAsync(ct) ?? 0) + 1;
        var pb = new PhienBanQuyen
        {
            SVTrongDotId = hoSo.Id,
            SoPhienBan = soPhienBan,
            LoaiBan = loai,
            FileId = tep.Id,
            HashSHA256 = luu.HashSHA256,
            KichThuocByte = luu.KichThuocByte,
            NgayNop = now,
            NopQuaHan = clock.Today > hoSo.Dot.NgayNopQuyenHan,
            LaBanBaoVe = false,
        };
        db.PhienBanQuyen.Add(pb);
        await db.SaveChangesAsync(ct);

        // Chuyển trạng thái: nộp quyển → T07; nộp bản sửa gấp → T09 (điều kiện: bản mới hơn bản đã đọc).
        var maChuyen = loai == LoaiBanQuyen.NopQuyen ? "T07" : "T09";
        await workflow.TransitionAsync(hoSo.Id, new WorkflowTransitionRequest(Convert.ToBase64String(hoSo.RowVersion),
            MaChuyen: maChuyen, LyDo: loai == LoaiBanQuyen.NopQuyen ? "Nộp quyển." : "Nộp bản sửa gấp."), ct);

        if (quyenOptions.Value.DaoVanTuDong && await ThamSoDaoVanAsync(hoSo, ct) is { Bat: true } tsAuto)
        {
            var quet = await checker.QuetAsync(await files.DocAsync(luu.DuongDan, ct), ct);
            await GhiDaoVanAsync(pb.Id, quet.PhanMemQuet, quet.TyLeTrungPct, tsAuto.NguongPhanTram, ct);
        }
        return ToPhienBan(pb, tep.TenGoc, hoSo.Id);
    }

    public async Task<IReadOnlyList<PhienBanQuyenItem>> LichSuPhienBanAsync(long svTrongDotId, CancellationToken ct = default)
    {
        await KiemTraXemHoSoAsync(svTrongDotId, ct);
        var pbs = await db.PhienBanQuyen.AsNoTracking().Where(x => x.SVTrongDotId == svTrongDotId).OrderBy(x => x.SoPhienBan).ToListAsync(ct);
        var fileIds = pbs.Select(x => x.FileId).ToArray();
        var tenFile = await db.TepDinhKem.AsNoTracking().Where(t => fileIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.TenGoc, ct);
        return pbs.Select(pb => ToPhienBan(pb, tenFile.GetValueOrDefault(pb.FileId, string.Empty), svTrongDotId)).ToArray();
    }

    public async Task<(Stream Noi, string ContentType, string TenFile)> TaiPhienBanAsync(long phienBanId, CancellationToken ct = default)
    {
        var pb = await db.PhienBanQuyen.AsNoTracking().SingleOrDefaultAsync(x => x.Id == phienBanId, ct)
            ?? throw Loi("UPM_QUYEN_NOT_FOUND", "Không tìm thấy phiên bản quyển.");
        await KiemTraXemHoSoAsync(pb.SVTrongDotId, ct);
        var tep = await db.TepDinhKem.AsNoTracking().SingleAsync(x => x.Id == pb.FileId, ct);
        return (await files.DocAsync(tep.DuongDan, ct), tep.ContentType, tep.TenGoc);
    }

    // ------------------------------------------------------------------ Ý kiến GVHD (D05)

    public async Task<YKienGvhdItem> YKienGvhdAsync(long svTrongDotId, YKienGvhdRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var hoSo = await LayHoSoGvAsync(svTrongDotId, VaiTroPhanCong.GVHD, ct);
        if (hoSo.TrangThai != TrangThaiSVTrongDot.DA_NOP_QUYEN)
            throw Loi("UPM_QUYEN_TRANG_THAI", "Ý kiến GVHD chỉ ghi khi quyển đã nộp.");
        var kqLyDo = QuyenRules.KiemTraLyDoKhongDongY(request.DongY, request.LyDoKhongDongY);
        if (!kqLyDo.Dat) throw Loi("UPM_D05_THIEU_LY_DO", kqLyDo.LyDo);
        var lyDo = string.IsNullOrWhiteSpace(request.LyDoKhongDongY) ? null : request.LyDoKhongDongY.Trim();
        if (lyDo?.Length > 500) throw Loi("UPM_LY_DO_QUA_DAI", "Lý do tối đa 500 ký tự.");

        var tsBm = await ThamSoBoMonAsync(hoSo.BoMonGv, ct);
        if (request.DiemKhac is not null && tsBm?.TachDiemKhac != true)
            throw Loi("UPM_DIEM_KHAC_KHONG_DUOC_PHEP", "Bộ môn không tách điểm khác (ThamSoBoMon.TachDiemKhac).");
        if (request.DiemGVHD is { } d && d is < 0 or > 10) throw Loi("UPM_DIEM_NGOAI_THANG_10", "Điểm phải từ 0 đến 10.");

        var yKien = new YKienGVHD
        {
            SVTrongDotId = hoSo.Id,
            GiangVienId = hoSo.GiangVienId,
            DongY = request.DongY,
            LyDoKhongDongY = request.DongY ? null : lyDo,
            DiemGVHD = request.DiemGVHD,
            DiemKhac = request.DiemKhac,
            NgayCho = clock.Now.UtcDateTime,
        };
        db.YKienGVHD.Add(yKien);
        await db.SaveChangesAsync(ct);

        if (!request.DongY)
        {
            // D05/D07: GVHD không đồng ý → PHAI_LAM_LAI (T12) với lý do.
            var row = await db.SVTrongDot.SingleAsync(x => x.Id == hoSo.Id, ct);
            await workflow.TransitionAsync(row.Id, new WorkflowTransitionRequest(Convert.ToBase64String(row.RowVersion),
                MaChuyen: "T12", LyDo: lyDo), ct);
        }
        return new YKienGvhdItem(yKien.Id, yKien.SVTrongDotId, yKien.DongY, yKien.LyDoKhongDongY, yKien.DiemGVHD, yKien.DiemKhac, yKien.NgayCho);
    }

    // ------------------------------------------------------------------ Đọc duyệt GVĐD (D06, D08, D10)

    public async Task<DocDuyetItem> DocDuyetAsync(long svTrongDotId, DocDuyetRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var hoSo = await LayHoSoGvAsync(svTrongDotId, VaiTroPhanCong.GVDD, ct);
        if (hoSo.TrangThai != TrangThaiSVTrongDot.DA_NOP_QUYEN)
            throw Loi("UPM_QUYEN_TRANG_THAI", "Chỉ đọc duyệt khi quyển đã nộp.");
        var pb = await BanMoiNhatAsync(hoSo.Id, ct) ?? throw Loi("UPM_QUYEN_CHUA_NOP", "Hồ sơ chưa có quyển nộp.");

        var truoc = await db.KetQuaDocDuyet.AsNoTracking().Where(x => x.SVTrongDotId == hoSo.Id)
            .OrderByDescending(x => x.NgayQuyet).ThenByDescending(x => x.Id).Select(x => new { x.VongDoc }).FirstOrDefaultAsync(ct);
        var vong = (byte)Math.Min(byte.MaxValue, (truoc?.VongDoc ?? 0) + 1);

        var kqD10 = QuyenRules.KiemTraDiemGVDD(request.KetQua, request.DiemGVDD);
        if (!kqD10.Dat) throw Loi("UPM_D10_THIEU_DIEM_GVDD", kqD10.LyDo);
        if (request.DiemGVDD is { } d && d is < 0 or > 10) throw Loi("UPM_DIEM_NGOAI_THANG_10", "Điểm phải từ 0 đến 10.");
        var nhanXet = string.IsNullOrWhiteSpace(request.NhanXet) ? null : request.NhanXet.Trim();
        if (nhanXet?.Length > 2000) throw Loi("UPM_NHAN_XET_QUA_DAI", "Nhận xét tối đa 2.000 ký tự.");

        if (request.KetQua == KetQuaDocDuyetLoai.SuaGap)
        {
            var tsGvdd = await ThamSoBoMonAsync(hoSo.BoMonGv, ct)
                ?? throw Loi("UPM_THAM_SO_CHUA_CAU_HINH", "Bộ môn chưa cấu hình ThamSoBoMon (số ngày sửa gấp).");
            var han = request.HanSuaDen ?? throw Loi("UPM_D06_HAN_SUA_KHONG_HOP_LE", "Sửa gấp phải nhập hạn sửa.");
            var kqSua = QuyenRules.KiemTraSuaGap(vong, clock.Today, han, tsGvdd.SoNgaySuaGapToiDa);
            if (!kqSua.Dat)
                throw Loi(kqSua.MaNgoaiLe == "D08" ? "UPM_D08_HET_VONG_SUA_GAP" : "UPM_D06_HAN_SUA_KHONG_HOP_LE", kqSua.LyDo);
        }
        if (request.KetQua == KetQuaDocDuyetLoai.KhongDongY && nhanXet is null)
            throw Loi("UPM_D07_THIEU_NHAN_XET", "Không đồng ý cho bảo vệ phải ghi nhận xét (lý do).");

        var doc = new KetQuaDocDuyet
        {
            SVTrongDotId = hoSo.Id,
            PhienBanQuyenId = pb.Id,
            GVDDId = hoSo.GiangVienId,
            VongDoc = vong,
            KetQua = request.KetQua,
            NhanXet = nhanXet,
            DiemGVDD = request.DiemGVDD,
            HanSuaDen = request.HanSuaDen,
            NgayQuyet = clock.Now.UtcDateTime,
        };
        db.KetQuaDocDuyet.Add(doc);
        await db.SaveChangesAsync(ct);

        var row = await db.SVTrongDot.SingleAsync(x => x.Id == hoSo.Id, ct);
        switch (request.KetQua)
        {
            case KetQuaDocDuyetLoai.SuaGap:
                await workflow.TransitionAsync(row.Id, new WorkflowTransitionRequest(Convert.ToBase64String(row.RowVersion),
                    MaChuyen: "T08"), ct);
                break;
            case KetQuaDocDuyetLoai.KhongDongY:
                await workflow.TransitionAsync(row.Id, new WorkflowTransitionRequest(Convert.ToBase64String(row.RowVersion),
                    MaChuyen: "T12", LyDo: nhanXet), ct);
                break;
        }
        return new DocDuyetItem(doc.Id, doc.SVTrongDotId, doc.PhienBanQuyenId, pb.SoPhienBan, doc.VongDoc, doc.KetQua, doc.NhanXet,
            doc.DiemGVDD, doc.HanSuaDen, doc.NgayQuyet);
    }

    public async Task<IReadOnlyList<QuyenCanDocItem>> QuyenCanDocAsync(long dotId, CancellationToken ct = default)
    {
        var gv = GiangVienHienTai();
        var ids = await db.PhanCong.AsNoTracking()
            .Where(p => p.GiangVienId == gv && p.VaiTro == VaiTroPhanCong.GVDD && p.NgayKetThuc == null && p.SVTrongDot.DotId == dotId
                && p.SVTrongDot.TrangThai == TrangThaiSVTrongDot.DA_NOP_QUYEN)
            .Select(p => p.SVTrongDotId).ToListAsync(ct);
        var ngayHan = clock.Today;
        var soNgay = quyenOptions.Value.SoNgayGVDDDoc;
        var ketQua = new List<QuyenCanDocItem>();
        foreach (var id in ids)
        {
            var pb = await BanMoiNhatAsync(id, ct);
            if (pb is null || await db.KetQuaDocDuyet.AnyAsync(x => x.PhienBanQuyenId == pb.Id, ct)) continue;
            var sv = await db.SVTrongDot.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.SinhVien.MaSV, x.SinhVien.HoTen }).SingleAsync(ct);
            var tep = await db.TepDinhKem.AsNoTracking().Where(t => t.Id == pb.FileId).Select(t => t.TenGoc).SingleAsync(ct);
            var truoc = await db.KetQuaDocDuyet.AsNoTracking().Where(x => x.SVTrongDotId == id).OrderByDescending(x => x.NgayQuyet)
                .Select(x => (int?)x.VongDoc).FirstOrDefaultAsync(ct);
            var han = DateOnly.FromDateTime(pb.NgayNop).AddDays(soNgay);
            ketQua.Add(new QuyenCanDocItem(id, sv.MaSV, sv.HoTen, pb.Id, pb.SoPhienBan, pb.LoaiBan, tep, pb.NgayNop, han,
                han.DayNumber - ngayHan.DayNumber, (truoc ?? 0) + 1));
        }
        return ketQua.OrderBy(x => x.HanDoc).ThenBy(x => x.MaSV).ToArray();
    }

    // ------------------------------------------------------------------ Đạo văn (D11) và bảo vệ (D13)

    public async Task<KiemTraDaoVanItem> NhapTayDaoVanAsync(long svTrongDotId, NhapTayDaoVanRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await KiemTraQuyenPhanCongAsync(ct);
        var hoSo = await db.SVTrongDot.Include(x => x.CTDT).SingleOrDefaultAsync(x => x.Id == svTrongDotId, ct)
            ?? throw Loi("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ.");
        var pb = await BanMoiNhatAsync(hoSo.Id, ct) ?? throw Loi("UPM_QUYEN_CHUA_NOP", "Hồ sơ chưa có quyển nộp.");
        var ts = await ThamSoDaoVanAsync(hoSo, ct)
            ?? throw Loi("UPM_DAO_VAN_CHUA_BAT", "Khoa × chương trình chưa bật kiểm tra đạo văn (ThamSoDaoVan.Bat).");
        if (!ts.Bat) throw Loi("UPM_DAO_VAN_CHUA_BAT", "Khoa × chương trình chưa bật kiểm tra đạo văn (ThamSoDaoVan.Bat).");
        if (request.TyLeTrungPct is < 0 or > 100) throw Loi("UPM_TY_LE_KHONG_HOP_LE", "Tỉ lệ trùng từ 0 đến 100%.");
        if (string.IsNullOrWhiteSpace(request.PhanMemQuet) || request.PhanMemQuet.Length > 100)
            throw Loi("UPM_PHAN_MEM_KHONG_HOP_LE", "Nhập tên phần mềm quét (tối đa 100 ký tự).");
        var kq = await GhiDaoVanAsync(pb.Id, request.PhanMemQuet.Trim(), request.TyLeTrungPct, ts.NguongPhanTram, ct);
        return kq;
    }

    public async Task<KiemTraBaoVeItem> KiemTraBaoVeAsync(long svTrongDotId, CancellationToken ct = default)
    {
        await KiemTraXemHoSoAsync(svTrongDotId, ct);
        var lyDo = new List<string>();
        var pb = await BanMoiNhatAsync(svTrongDotId, ct);
        if (pb is null) return new KiemTraBaoVeItem(false, ["Chưa có quyển nộp."]);

        var dao = await db.KetQuaKiemTraDaoVan.AsNoTracking().Where(x => x.PhienBanQuyenId == pb.Id)
            .OrderByDescending(x => x.NgayQuet).FirstOrDefaultAsync(ct);
        if (dao is { VuotNguong: true })
            lyDo.Add($"D11: tỉ lệ trùng {dao.TyLeTrungPct}% vượt ngưỡng {dao.NguongApDungPct}% – chặn bảo vệ, mở hồ sơ liêm chính.");

        var doc = await db.KetQuaDocDuyet.AsNoTracking().Where(x => x.SVTrongDotId == svTrongDotId)
            .OrderByDescending(x => x.NgayQuyet).ThenByDescending(x => x.Id).FirstOrDefaultAsync(ct);
        if (doc?.KetQua != KetQuaDocDuyetLoai.DongY)
            lyDo.Add("GVĐD chưa đồng ý cho bảo vệ (K3).");

        var marked = await db.PhienBanQuyen.AsNoTracking().Where(x => x.SVTrongDotId == svTrongDotId && x.LaBanBaoVe)
            .Select(x => new { x.HashSHA256 }).FirstOrDefaultAsync(ct);
        if (marked is null) lyDo.Add("Chưa đánh dấu bản bảo vệ.");
        else
        {
            var kqHash = QuyenRules.KiemTraBanBaoVe(pb.HashSHA256, marked.HashSHA256);
            if (!kqHash.Dat) lyDo.Add(kqHash.LyDo);
        }
        return new KiemTraBaoVeItem(lyDo.Count == 0, lyDo);
    }

    public async Task<BanBaoVeItem> DanhDauBanBaoVeAsync(long svTrongDotId, CancellationToken ct = default)
    {
        await KiemTraQuyenPhanCongAsync(ct);
        var hoSo = await db.SVTrongDot.SingleOrDefaultAsync(x => x.Id == svTrongDotId, ct)
            ?? throw Loi("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ.");
        // Bản bảo vệ phải là bản GVĐD đã đọc và đồng ý (D13).
        var doc = await db.KetQuaDocDuyet.AsNoTracking().Where(x => x.SVTrongDotId == hoSo.Id && x.KetQua == KetQuaDocDuyetLoai.DongY)
            .OrderByDescending(x => x.NgayQuyet).ThenByDescending(x => x.Id).FirstOrDefaultAsync(ct)
            ?? throw Loi("UPM_D13_CHUA_DOC_DUYET_DONG_Y", "Chỉ đánh dấu bản bảo vệ khi GVĐD đã đọc và đồng ý.");
        var pbDuyet = await db.PhienBanQuyen.SingleAsync(x => x.Id == doc.PhienBanQuyenId, ct);
        var toanBo = await db.PhienBanQuyen.Where(x => x.SVTrongDotId == hoSo.Id).ToListAsync(ct);
        foreach (var v in toanBo) v.LaBanBaoVe = v.Id == pbDuyet.Id;
        await db.SaveChangesAsync(ct);
        return new BanBaoVeItem(pbDuyet.Id, pbDuyet.SoPhienBan, pbDuyet.HashSHA256);
    }

    // ------------------------------------------------------------------ GVĐD trễ hạn (D09)

    public async Task<IReadOnlyList<GvddTreHanItem>> GvddTreHanAsync(long dotId, CancellationToken ct = default)
    {
        await KiemTraQuyenPhanCongAsync(ct);
        var khoa = await KhoaBmAsync(ct);
        var hoSos = await db.SVTrongDot.AsNoTracking()
            .Where(x => x.DotId == dotId && x.TrangThai == TrangThaiSVTrongDot.DA_NOP_QUYEN && (khoa == null || khoa.Contains(x.CTDT.KhoaId)))
            .Select(x => new { x.Id, x.SinhVien.MaSV, x.SinhVien.HoTen }).ToListAsync(ct);
        var ketQua = new List<GvddTreHanItem>();
        foreach (var h in hoSos)
        {
            var pb = await BanMoiNhatAsync(h.Id, ct);
            if (pb is null || await db.KetQuaDocDuyet.AnyAsync(x => x.PhienBanQuyenId == pb.Id, ct)) continue;
            if (!QuyenRules.QuaHanDocDuyet(pb.NgayNop, clock.Now.UtcDateTime, quyenOptions.Value.SoNgayGVDDDoc)) continue;
            var gvdd = await db.PhanCong.AsNoTracking().Where(p => p.SVTrongDotId == h.Id && p.VaiTro == VaiTroPhanCong.GVDD && p.NgayKetThuc == null)
                .Select(p => new { p.GiangVienId, p.GiangVien.MaGV, p.GiangVien.HoTen }).FirstOrDefaultAsync(ct);
            if (gvdd is null) continue;
            var soNgay = (clock.Now.UtcDateTime - pb.NgayNop).Days - quyenOptions.Value.SoNgayGVDDDoc;
            ketQua.Add(new GvddTreHanItem(h.Id, h.MaSV, h.HoTen, gvdd.GiangVienId, gvdd.MaGV, gvdd.HoTen, pb.NgayNop, Math.Max(1, soNgay)));
        }
        return ketQua.OrderByDescending(x => x.SoNgayQuaHan).ToArray();
    }

    /// <summary>D09: GVĐD quá hạn → Trưởng BM thay GVĐD (đóng dòng cũ, mở dòng mới). Không đổi trạng thái SV.</summary>
    public async Task ThayGvddAsync(long svTrongDotId, ThayGvddRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await KiemTraQuyenPhanCongAsync(ct);
        var khoa = await KhoaBmAsync(ct);
        var hoSo = await db.SVTrongDot.SingleOrDefaultAsync(x => x.Id == svTrongDotId && (khoa == null || khoa.Contains(x.CTDT.KhoaId)), ct)
            ?? throw Loi("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ trong phạm vi bộ môn của bạn.");
        var pb = await BanMoiNhatAsync(hoSo.Id, ct) ?? throw Loi("UPM_QUYEN_CHUA_NOP", "Hồ sơ chưa có quyển nộp.");
        if (await db.KetQuaDocDuyet.AnyAsync(x => x.PhienBanQuyenId == pb.Id, ct))
            throw Loi("UPM_D09_KHONG_QUA_HAN", "Quyển đã được GVĐD đọc; không thay GVĐD.");
        if (!QuyenRules.QuaHanDocDuyet(pb.NgayNop, clock.Now.UtcDateTime, quyenOptions.Value.SoNgayGVDDDoc))
            throw Loi("UPM_D09_CHUA_QUA_HAN", "GVĐD chưa quá hạn đọc quyển.");
        if (string.IsNullOrWhiteSpace(request.LyDo)) throw Loi("UPM_D09_LY_DO_BAT_BUOC", "Thay GVĐD phải ghi lý do.");

        var gvhd = await db.PhanCong.Where(p => p.SVTrongDotId == hoSo.Id && p.VaiTro == VaiTroPhanCong.GVHD && p.NgayKetThuc == null)
            .Select(p => p.GiangVienId).FirstOrDefaultAsync(ct);
        var gvMoi = await db.GiangVien.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.GiangVienMoiId && x.ConHoatDong, ct)
            ?? throw Loi("UPM_GV_NOT_FOUND", "GVĐD mới không tồn tại hoặc đã ngừng hoạt động.");
        var kqB06 = DeTaiRules.KiemTraTrungVaiTro(gvhd, gvMoi.Id, []);
        if (!kqB06.Dat) throw Loi("UPM_B06_GVDD_TRUNG_GVHD", kqB06.LyDo);

        var cu = await db.PhanCong.SingleOrDefaultAsync(p => p.SVTrongDotId == hoSo.Id && p.VaiTro == VaiTroPhanCong.GVDD && p.NgayKetThuc == null, ct)
            ?? throw Loi("UPM_PHAN_CONG_CHUA_CO_GVDD", "Hồ sơ chưa có GVĐD để thay.");
        var nay = clock.Today;
        cu.NgayKetThuc = nay;
        cu.LyDoThayDoi = Cat($"GVĐD quá hạn đọc (D09): {request.LyDo.Trim()}", 300);
        db.PhanCong.Add(new PhanCong
        {
            SVTrongDotId = hoSo.Id,
            GiangVienId = gvMoi.Id,
            VaiTro = VaiTroPhanCong.GVDD,
            NgayBatDau = nay,
            LyDoThayDoi = Cat($"Thay GVĐD (D09): {request.LyDo.Trim()}", 300),
            DuocNhapDiem = gvMoi.LoaiGV == LoaiGiangVien.CoHuu,
            TuKhaiXungDot = false,
        });
        await db.SaveChangesAsync(ct);
    }

    // ================================================================== Tiện ích

    private async Task<PhienBanQuyen?> BanMoiNhatAsync(long hoSoId, CancellationToken ct) =>
        await db.PhienBanQuyen.Where(x => x.SVTrongDotId == hoSoId).OrderByDescending(x => x.SoPhienBan).FirstOrDefaultAsync(ct);

    /// <summary>Hạn nộp quyển mới nhất được gia hạn (đơn GiaHan đã duyệt) – "mở lại riêng" có log qua đơn (D12).</summary>
    private async Task<DateOnly?> HanGiaHanAsync(long hoSoId, CancellationToken ct) =>
        await db.DonTu.AsNoTracking()
            .Where(d => d.SVTrongDotId == hoSoId && d.LoaiDon == LoaiDon.GiaHan && d.TrangThai == TrangThaiDonTu.DaDuyet && d.HanMoi != null)
            .OrderByDescending(d => d.NgayXuLy).ThenByDescending(d => d.Id)
            .Select(d => d.HanMoi).FirstOrDefaultAsync(ct);

    private async Task<ThamSoDaoVan?> ThamSoDaoVanAsync(SVTrongDot hoSo, CancellationToken ct)
    {
        var khoaId = await db.ChuongTrinhDaoTao.Where(x => x.Id == hoSo.CTDTId).Select(x => x.KhoaId).SingleAsync(ct);
        var chuongTrinh = await db.ChuongTrinhDaoTao.Where(x => x.Id == hoSo.CTDTId).Select(x => x.ChuongTrinh).SingleAsync(ct);
        return await db.ThamSoDaoVan.AsNoTracking().FirstOrDefaultAsync(x => x.KhoaId == khoaId && x.ChuongTrinh == chuongTrinh, ct);
    }

    private async Task<ThamSoBoMon?> ThamSoBoMonAsync(long? boMonId, CancellationToken ct) =>
        boMonId is null ? null : await db.ThamSoBoMon.AsNoTracking().FirstOrDefaultAsync(x => x.BoMonId == boMonId, ct);

    private async Task<KiemTraDaoVanItem> GhiDaoVanAsync(long phienBanId, string phanMem, decimal tyLe, decimal nguong, CancellationToken ct)
    {
        var kq = QuyenRules.KiemTraDaoVan(tyLe, nguong);
        var row = new KetQuaKiemTraDaoVan
        {
            PhienBanQuyenId = phienBanId,
            PhanMemQuet = phanMem,
            TyLeTrungPct = tyLe,
            NguongApDungPct = nguong,
            VuotNguong = !kq.Dat,
            NgayQuet = clock.Now.UtcDateTime,
        };
        db.KetQuaKiemTraDaoVan.Add(row);
        await db.SaveChangesAsync(ct);
        return new KiemTraDaoVanItem(row.Id, phienBanId, phanMem, tyLe, nguong, row.VuotNguong, row.NgayQuet);
    }

    private static PhienBanQuyenItem ToPhienBan(PhienBanQuyen pb, string tenFile, long hoSoId) => new(pb.Id, hoSoId, pb.SoPhienBan,
        pb.LoaiBan, tenFile, pb.KichThuocByte, pb.HashSHA256, pb.NgayNop, pb.NopQuaHan, pb.LaBanBaoVe);

    /// <summary>SV chính chủ, hoặc GVHD/GVĐD còn hiệu lực, hoặc PĐT/BM thuộc phạm vi.</summary>
    private async Task KiemTraXemHoSoAsync(long svTrongDotId, CancellationToken ct)
    {
        var hoSo = await db.SVTrongDot.AsNoTracking().SingleOrDefaultAsync(x => x.Id == svTrongDotId, ct)
            ?? throw Loi("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ.");
        if (currentUser.SinhVienId == hoSo.SinhVienId) return;
        if (currentUser.GiangVienId is { } gv
            && await db.PhanCong.AnyAsync(p => p.SVTrongDotId == hoSo.Id && p.GiangVienId == gv && p.NgayKetThuc == null, ct)) return;
        var q = await quyen.LayAsync(ct);
        if ((q.CoQuyen(Quyen.DotQuanLy) || q.CoQuyen(Quyen.PhanCongQuanLy)) && q.CoSoDuocPhep.Contains(hoSo.CoSoId)) return;
        throw Loi("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ trong phạm vi được phép.");
    }

    /// <summary>Hồ sơ + vai trò GV hiện tại (phải còn hiệu lực) + bộ môn của GV đó (tra ThamSoBoMon).</summary>
    private async Task<HoSoGv> LayHoSoGvAsync(long svTrongDotId, VaiTroPhanCong vaiTro, CancellationToken ct)
    {
        var gv = GiangVienHienTai();
        var row = await db.SVTrongDot.AsNoTracking().Where(x => x.Id == svTrongDotId)
            .Select(x => new { x.Id, x.TrangThai, Co = x.PhanCongs.Any(p => p.GiangVienId == gv && p.VaiTro == vaiTro && p.NgayKetThuc == null) })
            .SingleOrDefaultAsync(ct) ?? throw Loi("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ.");
        if (!row.Co) throw Loi("UPM_PERMISSION_DENIED", $"Chỉ {vaiTro} còn hiệu lực của hồ sơ này được thực hiện.");
        var boMon = await db.GiangVien.AsNoTracking().Where(x => x.Id == gv).Select(x => (long?)x.BoMonId).SingleAsync(ct);
        return new HoSoGv(row.Id, row.TrangThai, gv, boMon);
    }

    private sealed record HoSoGv(long Id, TrangThaiSVTrongDot TrangThai, long GiangVienId, long? BoMonGv);

    private async Task KiemTraQuyenPhanCongAsync(CancellationToken ct)
    {
        var q = await quyen.LayAsync(ct);
        if (!q.CoQuyen(Quyen.DotQuanLy) && !q.CoQuyen(Quyen.PhanCongQuanLy))
            throw Loi("UPM_PERMISSION_DENIED", "Chỉ Phòng Đào tạo hoặc Trưởng Bộ môn thực hiện thao tác này.");
    }

    private async Task<long[]?> KhoaBmAsync(CancellationToken ct)
    {
        var pv = (await quyen.LayAsync(ct)).PhamVi([VaiTroMa.TruongBoMon]);
        if (pv.ToanHeThong) return null;
        var boMonIds = pv.BoMonIds.ToArray();
        var khoa = await db.BoMon.AsNoTracking().Where(b => boMonIds.Contains(b.Id)).Select(b => b.KhoaId).ToArrayAsync(ct);
        return pv.KhoaIds.Concat(khoa).Distinct().ToArray();
    }

    private long GiangVienHienTai() => currentUser.GiangVienId ?? throw Loi("UPM_CHI_GIANG_VIEN", "Chức năng này chỉ dành cho giảng viên.");

    private long SinhVienHienTai() => currentUser.SinhVienId ?? throw Loi("UPM_CHI_SINH_VIEN", "Chức năng này chỉ dành cho sinh viên.");

    private static string Cat(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";

    private static DomainException Loi(string code, string message) => new(code, message);
}
