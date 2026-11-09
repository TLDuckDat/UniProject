using Microsoft.EntityFrameworkCore;
using Upm.Application.Abstractions;
using Upm.Application.DeTaiPhanCong;
using Upm.Application.PhanQuyen;
using Upm.Domain.Common;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Domain.PhanQuyen;
using Upm.Domain.Rules;
using Upm.Infrastructure.Persistence;

namespace Upm.Infrastructure.DeTaiPhanCong;

/// <summary>
/// UC-2 · Đề tài & phân công: đề xuất (B01, B02, B04, B10), nguyện vọng và duyệt SV (khóa khi đủ MaxSV),
/// phân công GVHD/GVĐD/đồng HD (B03, B05, B06, B07, B11), đổi GVHD / đổi đề tài sau QĐ (B08, B09; giữ lịch sử).
/// Quy tắc thuần nằm ở <see cref="DeTaiRules"/>; service chỉ dựng dữ liệu, kiểm quyền, ghi và giao dịch.
/// </summary>
public sealed class DeTaiService(
    UpmDbContext db,
    IScopeFilter scope,
    IQuyenHienTai quyen,
    ICurrentUser currentUser,
    IAuditContext auditContext,
    IClock clock) : IDeTaiService
{
    private const int SoNguyenVongToiDa = 5;
    private const int SoNgayDoiDeTaiMacDinh = 27; // tuần 3–4 (khi chưa cấu hình mốc M1)

    // ------------------------------------------------------------------ Đề tài

    public async Task<IReadOnlyList<DeTaiItem>> DanhSachAsync(
        long dotId, long? boMonId, TrangThaiDuyetDeTai? trangThai, string? q, CancellationToken ct = default)
    {
        IQueryable<DeTai> query;
        if (currentUser.SinhVienId is not null)
        {
            // Danh mục đề tài mở cho SV: chỉ đề tài đã được Bộ môn duyệt.
            query = db.DeTai.AsNoTracking().Where(x => x.DotId == dotId && x.TrangThaiDuyet == TrangThaiDuyetDeTai.DaDuyet);
        }
        else
        {
            query = (await scope.LocAsync(db.DeTai.AsNoTracking(), ct: ct)).Where(x => x.DotId == dotId);
            if (trangThai is not null) query = query.Where(x => x.TrangThaiDuyet == trangThai);
        }
        if (boMonId is not null) query = query.Where(x => x.BoMonId == boMonId);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.TenDeTai.Contains(term) || x.BoMon.TenBM.Contains(term));
        }
        return await ProjectAsync(query, ct);
    }

    public async Task<IReadOnlyList<DeTaiItem>> DeTaiCuaToiAsync(long dotId, CancellationToken ct = default)
    {
        var gv = GiangVienHienTai();
        return await ProjectAsync(db.DeTai.AsNoTracking().Where(x => x.DotId == dotId && x.GVDeXuatId == gv), ct);
    }

    /// <summary>Danh sách GV đang hoạt động của cơ sở đợt – để SV chọn GV đề nghị khi tự đề xuất (B04).</summary>
    public async Task<IReadOnlyList<GiangVienTomTat>> DanhSachGvDeNghiAsync(long dotId, CancellationToken ct = default)
    {
        var coSoId = await db.DotDATN.Where(x => x.Id == dotId).Select(x => (long?)x.CoSoId).SingleOrDefaultAsync(ct)
            ?? throw Loi("UPM_DOT_NOT_FOUND", "Không tìm thấy đợt.");
        if (currentUser.SinhVienId is { } sv)
        {
            if (!await db.SVTrongDot.AnyAsync(x => x.DotId == dotId && x.SinhVienId == sv, ct))
                throw Loi("UPM_DE_TAI_HO_SO_NOT_FOUND", "Bạn chưa có hồ sơ trong đợt này.");
        }
        else if (!(await quyen.LayAsync(ct)).CoSoDuocPhep.Contains(coSoId))
        {
            throw Loi("UPM_DOT_NOT_FOUND", "Không tìm thấy đợt trong phạm vi được phép.");
        }
        return await db.GiangVien.AsNoTracking().Where(x => x.CoSoId == coSoId && x.ConHoatDong)
            .OrderBy(x => x.MaGV).Select(x => new GiangVienTomTat(x.Id, x.MaGV, x.HoTen, x.LoaiGV)).ToListAsync(ct);
    }

    /// <summary>Bộ môn của cơ sở đợt (để chọn khi đề xuất / lọc danh mục đề tài).</summary>
    public async Task<IReadOnlyList<BoMonTomTat>> DanhSachBoMonAsync(long dotId, CancellationToken ct = default)
    {
        var coSoId = await CoSoCuaDotAsync(dotId, ct);
        return await db.BoMon.AsNoTracking().Where(x => x.Khoa.CoSoId == coSoId)
            .OrderBy(x => x.Khoa.MaKhoa).ThenBy(x => x.MaBM)
            .Select(x => new BoMonTomTat(x.Id, x.MaBM, x.TenBM, x.Khoa.TenKhoa)).ToListAsync(ct);
    }

    /// <summary>Các đợt người dùng được xem: theo cơ sở được phép; SV thêm các đợt có hồ sơ của mình.</summary>
    public async Task<IReadOnlyList<DotTomTat>> DanhSachDotAsync(CancellationToken ct = default)
    {
        var coSo = (await quyen.LayAsync(ct)).CoSoDuocPhep;
        var sv = currentUser.SinhVienId;
        var dotIds = sv is null ? null : await db.SVTrongDot.Where(x => x.SinhVienId == sv).Select(x => x.DotId).ToArrayAsync(ct);
        return await db.DotDATN.AsNoTracking()
            .Where(x => coSo.Contains(x.CoSoId) || (dotIds != null && dotIds.Contains(x.Id)))
            .OrderByDescending(x => x.NgayBatDau).ThenBy(x => x.MaDot)
            .Select(x => new DotTomTat(x.Id, x.CoSoId, x.MaDot, x.TenDot, x.NamHoc, x.HocKy, x.TrangThai.ToString()))
            .ToListAsync(ct);
    }

    private async Task<long> CoSoCuaDotAsync(long dotId, CancellationToken ct)
    {
        var coSoId = await db.DotDATN.Where(x => x.Id == dotId).Select(x => (long?)x.CoSoId).SingleOrDefaultAsync(ct)
            ?? throw Loi("UPM_DOT_NOT_FOUND", "Không tìm thấy đợt.");
        if (currentUser.SinhVienId is { } sv)
        {
            if (!await db.SVTrongDot.AnyAsync(x => x.DotId == dotId && x.SinhVienId == sv, ct))
                throw Loi("UPM_DE_TAI_HO_SO_NOT_FOUND", "Bạn chưa có hồ sơ trong đợt này.");
        }
        else if (!(await quyen.LayAsync(ct)).CoSoDuocPhep.Contains(coSoId))
        {
            throw Loi("UPM_DOT_NOT_FOUND", "Không tìm thấy đợt trong phạm vi được phép.");
        }
        return coSoId;
    }

    public async Task<DeTaiItem> GvDeXuatAsync(DeTaiTaoRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var gv = GiangVienHienTai();
        await KiemTraQuyenAsync(Quyen.DeTaiDeXuat, ct);
        var dot = await DotMoAsync(request.DotId, ct);
        var ten = KiemTraTen(request.TenDeTai);
        var moTa = KiemTraMoTa(request.MoTa);
        await KiemTraBoMonAsync(request.BoMonId, dot.CoSoId, ct);
        KiemTraSoSV(request.LoaiDeTai, request.MaxSV);

        var (canhBao, trung) = await KiemTraTrungAsync(dot, ten, moTa, ct);
        var deTai = new DeTai
        {
            DotId = dot.Id,
            BoMonId = request.BoMonId,
            TenDeTai = ten,
            MoTa = moTa,
            LoaiDeTai = request.LoaiDeTai,
            MaxSV = request.MaxSV,
            NguonDeXuat = NguonDeXuat.GV,
            GVDeXuatId = gv,
            // B01/B02: đề tài nhiều SV luôn chờ Bộ môn duyệt; đề tài 1 SV cũng qua Bộ môn (B04 quy trình chung).
            TrangThaiDuyet = TrangThaiDuyetDeTai.ChoDuyet,
            CanhBaoTrung = canhBao,
        };
        db.DeTai.Add(deTai);
        await db.SaveChangesAsync(ct);
        return await ItemAsync(deTai.Id, trung, ct);
    }

    public async Task<DeTaiItem> SvDeXuatAsync(SvDeXuatRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sv = SinhVienHienTai();
        await KiemTraQuyenAsync(Quyen.DeTaiDeXuat, ct);
        var hoSo = await db.SVTrongDot.Include(x => x.Dot).SingleOrDefaultAsync(x => x.Id == request.SVTrongDotId && x.SinhVienId == sv, ct)
            ?? throw Loi("UPM_DE_TAI_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ của sinh viên hiện tại.");
        var dot = await DotMoAsync(hoSo.DotId, ct);
        if (hoSo.DeTaiId is not null) throw Loi("UPM_DE_TAI_DA_CO_DE_TAI", "Bạn đã có đề tài trong đợt này.");
        var gv = await db.GiangVien.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.GiangVienDeNghiId && x.ConHoatDong, ct)
            ?? throw Loi("UPM_GV_NOT_FOUND", "Giảng viên được đề nghị không tồn tại hoặc đã ngừng hoạt động.");
        var ten = KiemTraTen(request.TenDeTai);
        var moTa = KiemTraMoTa(request.MoTa);
        KiemTraSoSV(request.LoaiDeTai, 1); // SV tự đề xuất luôn là đề tài 1 SV

        var (canhBao, trung) = await KiemTraTrungAsync(dot, ten, moTa, ct);
        var deTai = new DeTai
        {
            DotId = dot.Id,
            BoMonId = gv.BoMonId,
            TenDeTai = ten,
            MoTa = moTa,
            LoaiDeTai = request.LoaiDeTai,
            MaxSV = 1,
            NguonDeXuat = NguonDeXuat.SV,
            SVDeXuatId = sv,
            GVDeXuatId = gv.Id,
            // B04: "Nhap" = đang chờ GV được đề nghị xác nhận; sau đó mới tới Bộ môn duyệt.
            TrangThaiDuyet = TrangThaiDuyetDeTai.Nhap,
            CanhBaoTrung = canhBao,
        };
        db.DeTai.Add(deTai);
        await db.SaveChangesAsync(ct);
        return await ItemAsync(deTai.Id, trung, ct);
    }

    public async Task<DeTaiItem> GvXacNhanSvDeXuatAsync(long deTaiId, bool dongY, CancellationToken ct = default)
    {
        var gv = GiangVienHienTai();
        await KiemTraQuyenAsync(Quyen.DeTaiDeXuat, ct);
        var deTai = await db.DeTai.SingleOrDefaultAsync(x => x.Id == deTaiId && x.NguonDeXuat == NguonDeXuat.SV
            && x.TrangThaiDuyet == TrangThaiDuyetDeTai.Nhap && x.GVDeXuatId == gv, ct)
            ?? throw Loi("UPM_DE_TAI_KHONG_CHO_XAC_NHAN", "Đề tài không ở trạng thái chờ giảng viên xác nhận, hoặc không phải của bạn.");
        // B04: GV chấp nhận → chuyển cho Bộ môn duyệt; từ chối → kết thúc.
        deTai.TrangThaiDuyet = dongY ? TrangThaiDuyetDeTai.ChoDuyet : TrangThaiDuyetDeTai.TuChoi;
        await db.SaveChangesAsync(ct);
        return await ItemAsync(deTai.Id, null, ct);
    }

    public async Task<DeTaiItem> BoMonDuyetAsync(long deTaiId, bool dongY, CancellationToken ct = default)
    {
        await KiemTraQuyenAsync(Quyen.DeTaiDuyet, ct);
        var deTai = await (await scope.LocAsync(db.DeTai.AsQueryable(), [VaiTroMa.TruongBoMon], ct))
            .SingleOrDefaultAsync(x => x.Id == deTaiId, ct)
            ?? throw Loi("UPM_DE_TAI_SCOPE_DENIED", "Đề tài không thuộc bộ môn bạn quản lý.");
        if (deTai.TrangThaiDuyet != TrangThaiDuyetDeTai.ChoDuyet)
            throw Loi("UPM_DE_TAI_KHONG_CHO_DUYET", "Đề tài không ở trạng thái chờ Bộ môn duyệt.");
        deTai.TrangThaiDuyet = dongY ? TrangThaiDuyetDeTai.DaDuyet : TrangThaiDuyetDeTai.TuChoi;
        deTai.NguoiDuyetId = currentUser.TaiKhoanId;
        deTai.NgayDuyet = clock.Now.UtcDateTime;
        await db.SaveChangesAsync(ct);
        return await ItemAsync(deTai.Id, null, ct);
    }

    // ------------------------------------------------------------------ Nguyện vọng & duyệt SV

    public async Task<IReadOnlyList<NguyenVongItem>> DangKyNguyenVongAsync(long svTrongDotId, NguyenVongRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sv = SinhVienHienTai();
        await KiemTraQuyenAsync(Quyen.DeTaiDeXuat, ct);
        var hoSo = await LayHoSoCuaSinhVienAsync(svTrongDotId, sv, ct, includeDot: true);
        if (hoSo.Dot.TrangThai != TrangThaiDot.Mo)
            throw Loi("UPM_NGUYEN_VONG_NGOAI_KHUNG", "Đợt đã đóng, không đăng ký nguyện vọng.");
        if (hoSo.TrangThai != TrangThaiSVTrongDot.CHO_XET)
            throw Loi("UPM_NGUYEN_VONG_TRANG_THAI", "Chỉ hồ sơ đang chờ xét mới đăng ký nguyện vọng.");
        if (hoSo.DeTaiId is not null)
            throw Loi("UPM_NGUYEN_VONG_DA_CO_DE_TAI", "Hồ sơ đã có đề tài; không đăng ký thêm nguyện vọng.");

        var ids = request.DeTaiIds ?? [];
        if (ids.Count is 0 || ids.Count > SoNguyenVongToiDa || ids.Distinct().Count() != ids.Count)
            throw Loi("UPM_NGUYEN_VONG_SO_LUONG", $"Đăng ký từ 1 đến {SoNguyenVongToiDa} nguyện vọng, không trùng nhau.");

        var deTais = await db.DeTai.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        foreach (var id in ids)
        {
            if (!deTais.TryGetValue(id, out var dt) || dt.DotId != hoSo.DotId || dt.TrangThaiDuyet != TrangThaiDuyetDeTai.DaDuyet)
                throw Loi("UPM_NGUYEN_VONG_DE_TAI_KHONG_HOP_LE", "Nguyện vọng phải là đề tài đã duyệt của cùng đợt.");
            if (await SoSVDangCoAsync(id, null, ct) >= dt.MaxSV)
                throw Loi("UPM_DE_TAI_DA_DU_SV", $"Đề tài \"{dt.TenDeTai}\" đã đủ số SV tối đa.");
        }

        // Thay danh sách cũ (chưa trúng); giữ dòng đã trúng nếu có.
        var cu = await db.DangKyNguyenVong.Where(x => x.SVTrongDotId == hoSo.Id && x.TrangThai != TrangThaiNguyenVong.TrungTuyen).ToListAsync(ct);
        db.DangKyNguyenVong.RemoveRange(cu);
        await db.SaveChangesAsync(ct);

        var now = clock.Now.UtcDateTime;
        for (var i = 0; i < ids.Count; i++)
        {
            db.DangKyNguyenVong.Add(new DangKyNguyenVong
            {
                SVTrongDotId = hoSo.Id,
                DeTaiId = ids[i],
                ThuTuNguyenVong = (byte)(i + 1),
                TrangThai = TrangThaiNguyenVong.ChoDuyet,
                NgayDangKy = now,
            });
        }
        await db.SaveChangesAsync(ct);
        return await LayNguyenVongAsync(hoSo.Id, ct);
    }

    public async Task<IReadOnlyList<NguyenVongItem>> LayNguyenVongAsync(long svTrongDotId, CancellationToken ct = default)
    {
        var sv = currentUser.SinhVienId;
        var query = db.DangKyNguyenVong.AsNoTracking().Where(x => x.SVTrongDotId == svTrongDotId);
        if (sv is not null)
        {
            if (!await db.SVTrongDot.AnyAsync(x => x.Id == svTrongDotId && x.SinhVienId == sv, ct))
                throw Loi("UPM_DE_TAI_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ của sinh viên hiện tại.");
        }
        else
        {
            var duoc = await scope.LocAsync(db.SVTrongDot.AsNoTracking(), ct: ct);
            if (!await duoc.AnyAsync(x => x.Id == svTrongDotId, ct))
                throw Loi("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ trong phạm vi được phép.");
        }
        return await query.OrderBy(x => x.ThuTuNguyenVong).Select(x => new NguyenVongItem(
            x.Id, x.DeTaiId, x.DeTai.TenDeTai, x.ThuTuNguyenVong, x.TrangThai, x.NgayDangKy)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DangKyNguyenVongItem>> DanhSachDangKyAsync(long deTaiId, CancellationToken ct = default)
    {
        var gv = GiangVienHienTai();
        await KiemTraDeTaiCuaGVAsync(deTaiId, gv, ct);
        return await db.DangKyNguyenVong.AsNoTracking().Where(x => x.DeTaiId == deTaiId)
            .OrderBy(x => x.SVTrongDot.SinhVien.MaSV)
            .Select(x => new DangKyNguyenVongItem(x.Id, x.DeTaiId, x.SVTrongDotId, x.SVTrongDot.SinhVien.MaSV,
                x.SVTrongDot.SinhVien.HoTen, x.ThuTuNguyenVong, x.TrangThai, x.NgayDangKy))
            .ToListAsync(ct);
    }

    public async Task<DangKyNguyenVongItem> GvDuyetDangKyAsync(long deTaiId, DuyetDangKyRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var gv = GiangVienHienTai();
        await KiemTraQuyenAsync(Quyen.DeTaiDeXuat, ct);
        var deTai = await KiemTraDeTaiCuaGVAsync(deTaiId, gv, ct);
        var dk = await db.DangKyNguyenVong.Include(x => x.SVTrongDot)
            .SingleOrDefaultAsync(x => x.DeTaiId == deTaiId && x.SVTrongDotId == request.SVTrongDotId
                && x.TrangThai == TrangThaiNguyenVong.ChoDuyet, ct)
            ?? throw Loi("UPM_NGUYEN_VONG_KHONG_TIM_THAY", "Không có nguyện vọng đang chờ duyệt của sinh viên này cho đề tài.");

        if (request.DongY)
        {
            if (dk.SVTrongDot.DeTaiId is not null)
                throw Loi("UPM_NGUYEN_VONG_DA_CO_DE_TAI", "Sinh viên đã trúng một đề tài khác.");
            // Khóa đề tài khi đủ MaxSV (B02): không nhận thêm khi đã đủ.
            if (await SoSVDangCoAsync(deTaiId, null, ct) >= deTai.MaxSV)
                throw Loi("UPM_DE_TAI_DA_DU_SV", "Đề tài đã đủ số SV tối đa; không nhận thêm sinh viên.");
            dk.TrangThai = TrangThaiNguyenVong.TrungTuyen;
            dk.SVTrongDot.DeTaiId = deTaiId;
            // Các nguyện vọng khác còn chờ của SV này tự thành Trượt.
            var khac = await db.DangKyNguyenVong.Where(x => x.SVTrongDotId == dk.SVTrongDotId && x.Id != dk.Id
                && x.TrangThai == TrangThaiNguyenVong.ChoDuyet).ToListAsync(ct);
            foreach (var x in khac) x.TrangThai = TrangThaiNguyenVong.Truot;
        }
        else
        {
            dk.TrangThai = TrangThaiNguyenVong.Truot;
        }
        await db.SaveChangesAsync(ct);
        var svInfo = await db.SinhVien.AsNoTracking().Where(x => x.Id == dk.SVTrongDot.SinhVienId)
            .Select(x => new { x.MaSV, x.HoTen }).SingleAsync(ct);
        return new DangKyNguyenVongItem(dk.Id, dk.DeTaiId, dk.SVTrongDotId, svInfo.MaSV, svInfo.HoTen,
            dk.ThuTuNguyenVong, dk.TrangThai, dk.NgayDangKy);
    }

    public async Task<long> KhaiBaoXungDotAsync(KhaiBaoXungDotRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var gv = GiangVienHienTai();
        await KiemTraQuyenAsync(Quyen.DeTaiDeXuat, ct);
        if (!await db.SinhVien.AnyAsync(x => x.Id == request.SinhVienId, ct))
            throw Loi("UPM_SINH_VIEN_NOT_FOUND", "Không tìm thấy sinh viên.");
        var lyDo = string.IsNullOrWhiteSpace(request.LyDo) ? null : request.LyDo.Trim();
        if (lyDo?.Length > 300) throw Loi("UPM_XUNG_DOT_LY_DO_QUA_DAI", "Lý do khai báo không quá 300 ký tự.");
        var ex = await db.KhaiBaoXungDot.SingleOrDefaultAsync(x => x.GiangVienId == gv && x.SinhVienId == request.SinhVienId, ct);
        if (ex is not null)
        {
            ex.LyDo = lyDo;
            await db.SaveChangesAsync(ct);
            return ex.Id;
        }
        var khaiBao = new KhaiBaoXungDot { GiangVienId = gv, SinhVienId = request.SinhVienId, LyDo = lyDo };
        db.KhaiBaoXungDot.Add(khaiBao);
        await db.SaveChangesAsync(ct);
        return khaiBao.Id;
    }

    // ------------------------------------------------------------------ Phân công (Trưởng BM)

    public async Task<PhanCongHoSoItem> PhanCongAsync(PhanCongRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await KiemTraQuyenAsync(Quyen.PhanCongQuanLy, ct);
        var laBoMon = (await quyen.LayAsync(ct)).CoVaiTro(VaiTroMa.TruongBoMon);
        if (request.VuotQuotaXacNhan && !laBoMon)
            throw Loi("UPM_B03_CHI_TRUONG_BO_MON", "Chỉ Trưởng Bộ môn được duyệt ngoại lệ vượt quota (B03).");

        var hoSo = await HoSoBoMonAsync(request.SVTrongDotId, ct);
        if (hoSo.TrangThai is not (TrangThaiSVTrongDot.CHO_XET or TrangThaiSVTrongDot.DA_GIAO or TrangThaiSVTrongDot.DANG_THUC_HIEN))
            throw Loi("UPM_PHAN_CONG_TRANG_THAI", "Chỉ phân công cho hồ sơ đang chờ xét, đã giao hoặc đang thực hiện.");
        if (await db.PhanCong.AnyAsync(p => p.SVTrongDotId == hoSo.Id && p.VaiTro == VaiTroPhanCong.GVHD && p.NgayKetThuc == null, ct))
            throw Loi("UPM_PHAN_CONG_DA_CO_GVHD", "Hồ sơ đã có GVHD. Muốn đổi, dùng đơn đổi GVHD (B08).");

        // B05: đề tài gắn khi phân công (nếu hồ sơ chưa có).
        if (request.DeTaiId is { } deTaiId)
        {
            if (hoSo.DeTaiId is { } hienTai && hienTai != deTaiId)
                throw Loi("UPM_PHAN_CONG_DOI_DE_TAI", "Hồ sơ đã gắn đề tài khác; đổi đề tài phải qua đơn điều chỉnh (B09).");
            var deTai = await db.DeTai.SingleOrDefaultAsync(x => x.Id == deTaiId && x.DotId == hoSo.DotId
                && x.TrangThaiDuyet == TrangThaiDuyetDeTai.DaDuyet, ct)
                ?? throw Loi("UPM_DE_TAI_KHONG_HOP_LE", "Đề tài phải đã được duyệt và thuộc cùng đợt.");
            if (hoSo.DeTaiId is null)
            {
                if (await SoSVDangCoAsync(deTaiId, hoSo.Id, ct) >= deTai.MaxSV)
                    throw Loi("UPM_DE_TAI_DA_DU_SV", "Đề tài đã đủ số SV tối đa.");
                hoSo.DeTaiId = deTaiId;
            }
        }
        else if (hoSo.DeTaiId is null)
        {
            throw Loi("UPM_B05_CHUA_CO_DE_TAI", "Hồ sơ chưa có đề tài: chọn đề tài khi phân công hoặc để sinh viên đăng ký nguyện vọng trước.");
        }

        var dongHd = (request.DongHDIds ?? []).Distinct().ToArray();
        var ids = new List<long> { request.GVHDId };
        if (request.GVDDId is { } gvdd) ids.Add(gvdd);
        ids.AddRange(dongHd);
        var gvs = await LayGiangVienAsync(ids, ct);
        var vuotQuota = await KiemTraBoGiangVienAsync(hoSo.Id, hoSo.DotId, hoSo.SinhVienId, gvs, request.GVHDId,
            request.GVDDId, dongHd, request.VuotQuotaXacNhan, ct);

        var nay = clock.Today;
        var qdNull = (long?)null;
        ThemPhanCong(hoSo.Id, request.GVHDId, VaiTroPhanCong.GVHD, gvs[request.GVHDId], nay, qdNull, null, vuotQuota);
        if (request.GVDDId is { } g) ThemPhanCong(hoSo.Id, g, VaiTroPhanCong.GVDD, gvs[g], nay, qdNull, null, vuotQuota);
        foreach (var d in dongHd) ThemPhanCong(hoSo.Id, d, VaiTroPhanCong.DongHD, gvs[d], nay, qdNull, null, vuotQuota);
        await db.SaveChangesAsync(ct);
        return (await HoSoPhanCongAsync(db.SVTrongDot.AsNoTracking().Where(x => x.Id == hoSo.Id), ct)).Single();
    }

    public async Task<PhanCongDotItem> PhanCongDotAsync(long dotId, CancellationToken ct = default)
    {
        await KiemTraQuyenAsync(Quyen.PhanCongQuanLy, ct);
        var khoa = await KhoaBmAsync(ct);
        var hoSos = db.SVTrongDot.AsNoTracking().Where(x => x.DotId == dotId && (khoa == null || khoa.Contains(x.CTDT.KhoaId)));
        var ds = await HoSoPhanCongAsync(hoSos, ct);
        var gvs = await (await scope.LocAsync(db.GiangVien.AsNoTracking(), [VaiTroMa.TruongBoMon], ct))
            .Where(x => x.ConHoatDong)
            .OrderBy(x => x.MaGV)
            .Select(x => new { x.Id, x.MaGV, x.HoTen, x.LoaiGV, x.QuotaToiDa })
            .ToListAsync(ct);
        var tai = await TaiTheoGiangVienAsync(dotId, gvs.Select(x => x.Id).ToArray(), null, ct);
        var taiItems = gvs.Select(x =>
        {
            var dangCo = tai.GetValueOrDefault(x.Id);
            return new TaiGiangVienItem(x.Id, x.MaGV, x.HoTen, x.LoaiGV, dangCo, x.QuotaToiDa,
                DeTaiRules.MucTai(dangCo, x.QuotaToiDa));
        }).ToArray();
        return new PhanCongDotItem(ds, taiItems);
    }

    public async Task<IReadOnlyList<GoiYPhanCongItem>> GoiYPhanCongAsync(long dotId, CancellationToken ct = default)
    {
        await KiemTraQuyenAsync(Quyen.PhanCongQuanLy, ct);
        // B05: chỉ GỢI Ý – không ghi gì; Trưởng BM xác nhận bằng phân công thường.
        var khoaGoiY = await KhoaBmAsync(ct);
        var chuaGvhd = await db.SVTrongDot.AsNoTracking()
            .Where(x => (khoaGoiY == null || khoaGoiY.Contains(x.CTDT.KhoaId)) && x.DotId == dotId && (x.TrangThai == TrangThaiSVTrongDot.CHO_XET || x.TrangThai == TrangThaiSVTrongDot.DA_GIAO)
                && !db.PhanCong.Any(p => p.SVTrongDotId == x.Id && p.VaiTro == VaiTroPhanCong.GVHD && p.NgayKetThuc == null))
            .OrderBy(x => x.SinhVien.MaSV)
            .Select(x => new { x.Id, x.SinhVienId, x.DeTaiId, x.SinhVien.MaSV, x.SinhVien.HoTen })
            .ToListAsync(ct);
        if (chuaGvhd.Count == 0) return [];

        var deTais = await (await scope.LocAsync(db.DeTai.AsNoTracking(), [VaiTroMa.TruongBoMon], ct))
            .Where(x => x.DotId == dotId && x.TrangThaiDuyet == TrangThaiDuyetDeTai.DaDuyet)
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.BoMonId, x.TenDeTai, x.MaxSV, DaCo = x.SVTrongDots.Count() })
            .ToListAsync(ct);
        var gvs = await (await scope.LocAsync(db.GiangVien.AsNoTracking(), [VaiTroMa.TruongBoMon], ct))
            .Where(x => x.ConHoatDong)
            .Select(x => new { x.Id, x.BoMonId, x.MaGV, x.HoTen, x.QuotaToiDa })
            .ToListAsync(ct);
        var tai = await TaiTheoGiangVienAsync(dotId, gvs.Select(x => x.Id).ToArray(), null, ct);
        var slotConLai = deTais.ToDictionary(x => x.Id, x => x.MaxSV - x.DaCo);
        var taiMoi = gvs.ToDictionary(x => x.Id, x => tai.GetValueOrDefault(x.Id));

        var ketQua = new List<GoiYPhanCongItem>();
        foreach (var hs in chuaGvhd)
        {
            var dt = hs.DeTaiId is { } cur ? deTais.FirstOrDefault(x => x.Id == cur)
                : deTais.FirstOrDefault(x => slotConLai.GetValueOrDefault(x.Id) > 0);
            long? gvId = null;
            if (dt is not null)
            {
                var nhom = gvs.Where(x => x.BoMonId == dt.BoMonId).Select(x => new TaiGiangVien(x.Id, taiMoi[x.Id], x.QuotaToiDa));
                gvId = DeTaiRules.GoiYGiangVien(nhom);
                if (gvId is { } chon)
                {
                    taiMoi[chon]++;
                    if (hs.DeTaiId is null) slotConLai[dt.Id]--;
                }
            }
            var gv = gvId is { } g ? gvs.First(x => x.Id == g) : null;
            ketQua.Add(new GoiYPhanCongItem(hs.Id, hs.MaSV, hs.HoTen, dt?.Id, dt?.TenDeTai, gvId, gv?.MaGV, gv?.HoTen));
        }
        return ketQua;
    }

    public async Task<IReadOnlyList<PhanCongLichSuItem>> LichSuPhanCongAsync(long svTrongDotId, CancellationToken ct = default)
    {
        await KiemTraQuyenAsync(Quyen.PhanCongQuanLy, ct);
        _ = await HoSoBoMonAsync(svTrongDotId, ct);
        return await db.PhanCong.AsNoTracking().Where(x => x.SVTrongDotId == svTrongDotId)
            .OrderBy(x => x.NgayBatDau).ThenBy(x => x.Id)
            .Select(x => new PhanCongLichSuItem(x.Id, x.VaiTro, x.GiangVien.MaGV, x.GiangVien.HoTen, x.NgayBatDau,
                x.NgayKetThuc, x.LyDoThayDoi, x.QuyetDinh != null ? x.QuyetDinh.SoQuyetDinh : null, x.VuotQuotaDuyetBoiId != null))
            .ToListAsync(ct);
    }

    // ------------------------------------------------------------------ Đổi GVHD / đổi đề tài (B08, B09)

    public async Task<long> TaoDonDieuChinhAsync(long svTrongDotId, DonDieuChinhTaoRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sv = SinhVienHienTai();
        await KiemTraQuyenAsync(Quyen.DeTaiDeXuat, ct);
        if (request.LoaiDon is not (LoaiDon.DoiGVHD or LoaiDon.DoiDeTai))
            throw Loi("UPM_DON_LOAI_KHONG_HOP_LE", "Chỉ hỗ trợ đơn đổi GVHD hoặc đổi đề tài.");
        var noiDung = (request.NoiDung ?? string.Empty).Trim();
        if (noiDung.Length is 0 or > 4000) throw Loi("UPM_DON_NOI_DUNG_KHONG_HOP_LE", "Nội dung đơn từ 1 đến 4.000 ký tự.");
        var hoSo = await LayHoSoCuaSinhVienAsync(svTrongDotId, sv, ct);
        if (hoSo.QuyetDinhGiaoId is null)
            throw Loi("UPM_DON_CHUA_CO_QUYET_DINH", "Chỉ gửi đơn điều chỉnh sau khi đã có quyết định giao (B08, B09).");
        if (hoSo.DeTaiId is null) throw Loi("UPM_DON_CHUA_CO_DE_TAI", "Hồ sơ chưa có đề tài.");
        if (await db.DonTu.AnyAsync(x => x.SVTrongDotId == hoSo.Id && x.LoaiDon == request.LoaiDon && x.TrangThai == TrangThaiDonTu.ChoXuLy, ct))
            throw Loi("UPM_DON_DANG_CHO_XU_LY", "Hồ sơ đã có đơn cùng loại đang chờ xử lý.");

        var don = new DonTu
        {
            SVTrongDotId = hoSo.Id,
            LoaiDon = request.LoaiDon,
            NoiDung = noiDung,
            NgayGui = clock.Now.UtcDateTime,
            TrangThai = TrangThaiDonTu.ChoXuLy,
        };
        db.DonTu.Add(don);
        await db.SaveChangesAsync(ct);
        return don.Id;
    }

    public async Task<IReadOnlyList<DonDieuChinhItem>> DanhSachDonDieuChinhAsync(long dotId, CancellationToken ct = default)
    {
        await KiemTraQuyenAsync(Quyen.PhanCongQuanLy, ct);
        var khoa = await KhoaBmAsync(ct);
        var query = db.DonTu.AsNoTracking()
            .Where(x => (khoa == null || khoa.Contains(x.SVTrongDot.CTDT.KhoaId)) && x.SVTrongDot.DotId == dotId && (x.LoaiDon == LoaiDon.DoiGVHD || x.LoaiDon == LoaiDon.DoiDeTai));
        var rows = await query.OrderByDescending(x => x.NgayGui).ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.LoaiDon,
                x.SVTrongDotId,
                x.SVTrongDot.SinhVien.MaSV,
                x.SVTrongDot.SinhVien.HoTen,
                x.NoiDung,
                x.NgayGui,
                x.TrangThai,
                x.KetQuaXuLy,
                DeTaiId = x.SVTrongDot.DeTaiId,
                TenDeTai = x.SVTrongDot.DeTai!.TenDeTai,
                GVHD = x.SVTrongDot.PhanCongs.Where(p => p.VaiTro == VaiTroPhanCong.GVHD && p.NgayKetThuc == null)
                    .Select(p => p.GiangVien.HoTen).FirstOrDefault(),
            }).ToListAsync(ct);
        return rows.Select(x => new DonDieuChinhItem(x.Id, x.LoaiDon, x.SVTrongDotId, x.MaSV, x.HoTen, x.NoiDung, x.NgayGui,
            x.TrangThai, x.KetQuaXuLy, x.DeTaiId, x.TenDeTai, x.GVHD)).ToArray();
    }

    public async Task DuyetDonDieuChinhAsync(long donTuId, DuyetDonDieuChinhRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await KiemTraQuyenAsync(Quyen.PhanCongQuanLy, ct);
        var khoaDon = await KhoaBmAsync(ct);
        var don = await db.DonTu.Where(x => khoaDon == null || khoaDon.Contains(x.SVTrongDot.CTDT.KhoaId))
            .SingleOrDefaultAsync(x => x.Id == donTuId && (x.LoaiDon == LoaiDon.DoiGVHD || x.LoaiDon == LoaiDon.DoiDeTai), ct)
            ?? throw Loi("UPM_DON_NOT_FOUND", "Không tìm thấy đơn điều chỉnh trong phạm vi được phép.");
        if (don.TrangThai != TrangThaiDonTu.ChoXuLy) throw Loi("UPM_DON_DA_XU_LY", "Đơn đã được xử lý.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var now = clock.Now.UtcDateTime;
        if (!request.DongY)
        {
            if (string.IsNullOrWhiteSpace(request.LyDo)) throw Loi("UPM_DON_LY_DO_TU_CHOI_BAT_BUOC", "Từ chối đơn phải ghi lý do.");
            don.TrangThai = TrangThaiDonTu.TuChoi;
            don.KetQuaXuLy = request.LyDo.Trim();
            don.NguoiXuLyId = currentUser.TaiKhoanId;
            don.NgayXuLy = now;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return;
        }

        var soQd = request.SoQuyetDinh?.Trim() ?? string.Empty;
        if (soQd.Length is 0 or > 50) throw Loi("UPM_QUYET_DINH_SO_KHONG_HOP_LE", "Số quyết định phải có từ 1 đến 50 ký tự.");
        var ngayKy = request.NgayKy ?? throw Loi("UPM_QUYET_DINH_NGAY_KY_BAT_BUOC", "Cần ngày ký quyết định điều chỉnh.");
        var hoSo = await db.SVTrongDot.Include(x => x.Dot).Include(x => x.SinhVien).Include(x => x.DeTai)
            .SingleAsync(x => x.Id == don.SVTrongDotId, ct);
        if (await db.QuyetDinh.AnyAsync(x => x.CoSoId == hoSo.CoSoId && x.SoQuyetDinh == soQd, ct))
            throw Loi("UPM_QUYET_DINH_TRUNG_SO", $"Số quyết định {soQd} đã được ghi nhận.");

        var qd = new QuyetDinh
        {
            CoSoId = hoSo.CoSoId,
            SoQuyetDinh = soQd,
            NgayKy = ngayKy,
            LoaiQD = LoaiQuyetDinh.DieuChinh,
            DaKhoaDuLieu = true,
            GhiChu = $"Điều chỉnh {(don.LoaiDon == LoaiDon.DoiGVHD ? "GVHD" : "đề tài")}: {hoSo.SinhVien.MaSV}.",
        };
        var soQdCu = auditContext.SoQDBatBuoc;
        try
        {
            auditContext.SoQDBatBuoc = soQd;
            db.QuyetDinh.Add(qd);
            await db.SaveChangesAsync(ct);

            if (don.LoaiDon == LoaiDon.DoiGVHD)
            {
                await DoiGVHDAsync(hoSo, request, qd, don.NoiDung, ct);
            }
            else
            {
                // B09: đổi tên / nội dung đề tài chỉ trước hạn (mốc M1, mặc định tuần 4).
                var m1 = await db.MocTienDo.AsNoTracking().Where(x => x.DotId == hoSo.DotId && x.MaMoc == MaMocTienDo.M1)
                    .Select(x => (DateOnly?)x.NgayHan).FirstOrDefaultAsync(ct);
                var han = m1 ?? hoSo.Dot.NgayBatDau.AddDays(SoNgayDoiDeTaiMacDinh);
                var kqHan = DeTaiRules.KiemTraHanDoiDeTai(clock.Today, han);
                if (!kqHan.Dat) throw Loi("UPM_B09_QUA_HAN_DOI_DE_TAI", kqHan.LyDo);
                var tenMoi = request.TenDeTaiMoi?.Trim();
                if (string.IsNullOrEmpty(tenMoi)) throw Loi("UPM_B09_THIEU_TEN_MOI", "Cần nhập tên đề tài mới.");
                if (tenMoi.Length > 500) throw Loi("UPM_B09_TEN_QUA_DAI", "Tên đề tài tối đa 500 ký tự.");
                if (hoSo.DeTai is null) throw Loi("UPM_DON_CHUA_CO_DE_TAI", "Hồ sơ chưa có đề tài.");
                hoSo.DeTai.TenDeTai = tenMoi;
                if (request.MoTaMoi is not null) hoSo.DeTai.MoTa = request.MoTaMoi.Trim();
            }

            don.TrangThai = TrangThaiDonTu.DaDuyet;
            don.QuyetDinhId = qd.Id;
            don.NguoiXuLyId = currentUser.TaiKhoanId;
            don.KetQuaXuLy = $"Đã duyệt; quyết định điều chỉnh số {soQd}.";
            don.NgayXuLy = now;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        finally
        {
            auditContext.SoQDBatBuoc = soQdCu;
        }
    }

    /// <summary>B08: đóng dòng GVHD cũ (NgayKetThuc, giữ lịch sử và điểm tiến độ), mở dòng mới gắn QĐ điều chỉnh.</summary>
    private async Task DoiGVHDAsync(SVTrongDot hoSo, DuyetDonDieuChinhRequest request, QuyetDinh qd, string lyDoDon, CancellationToken ct)
    {
        var gvMoiId = request.GiangVienMoiId ?? throw Loi("UPM_DON_CAN_GV_MOI", "Cần chọn giảng viên hướng dẫn mới.");
        var gvhdCu = await db.PhanCong.SingleOrDefaultAsync(x => x.SVTrongDotId == hoSo.Id && x.VaiTro == VaiTroPhanCong.GVHD
            && x.NgayKetThuc == null, ct) ?? throw Loi("UPM_PHAN_CONG_CHUA_CO_GVHD", "Hồ sơ chưa có GVHD để thay đổi.");
        if (gvhdCu.GiangVienId == gvMoiId) throw Loi("UPM_DON_GV_TRUNG_HIEN_TAI", "GVHD mới trùng GVHD hiện tại.");

        var gvddHienTai = await db.PhanCong.Where(x => x.SVTrongDotId == hoSo.Id && x.VaiTro == VaiTroPhanCong.GVDD && x.NgayKetThuc == null)
            .Select(x => (long?)x.GiangVienId).FirstOrDefaultAsync(ct);
        var dongHienTai = await db.PhanCong.Where(x => x.SVTrongDotId == hoSo.Id && x.VaiTro == VaiTroPhanCong.DongHD && x.NgayKetThuc == null)
            .Select(x => x.GiangVienId).ToArrayAsync(ct);
        var ids = new List<long> { gvMoiId };
        if (gvddHienTai is { } g) ids.Add(g);
        ids.AddRange(dongHienTai);
        var gvs = await LayGiangVienAsync(ids, ct);
        // Đổi GVHD dùng đúng luật phân công (B06, B07, B11, B03 nghiêm ngặt – không có ngoại lệ ở đây).
        await KiemTraBoGiangVienAsync(hoSo.Id, hoSo.DotId, hoSo.SinhVienId, gvs, gvMoiId, gvddHienTai, dongHienTai, false, ct);

        var lyDo = Cat($"Đổi GVHD theo QĐ {qd.SoQuyetDinh}: {lyDoDon}", 300);
        gvhdCu.NgayKetThuc = clock.Today;
        gvhdCu.LyDoThayDoi = lyDo;
        ThemPhanCong(hoSo.Id, gvMoiId, VaiTroPhanCong.GVHD, gvs[gvMoiId], clock.Today, qd.Id, lyDo, new HashSet<long>());
        await db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------ Lõi kiểm tra & tiện ích

    /// <summary>Kiểm B06, B07, B11, B03 cho bộ GV của một hồ sơ. Trả về các GV được duyệt vượt quota (B03).</summary>
    private async Task<HashSet<long>> KiemTraBoGiangVienAsync(
        long svTrongDotId, long dotId, long sinhVienId, Dictionary<long, GiangVien> gvs,
        long gvhd, long? gvdd, IReadOnlyList<long> dongHd, bool choPhepVuotQuota, CancellationToken ct)
    {
        var kqB06 = DeTaiRules.KiemTraTrungVaiTro(gvhd, gvdd, dongHd);
        if (!kqB06.Dat) throw Loi("UPM_B06_GVDD_TRUNG_GVHD", kqB06.LyDo);

        var ids = new List<long> { gvhd };
        if (gvdd is { } d) ids.Add(d);
        ids.AddRange(dongHd);
        var khaiBao = (await db.KhaiBaoXungDot.AsNoTracking()
            .Where(x => x.SinhVienId == sinhVienId && ids.Contains(x.GiangVienId)).Select(x => x.GiangVienId).ToListAsync(ct)).ToHashSet();
        var kqB07 = DeTaiRules.KiemTraXungDot(ids, khaiBao);
        if (!kqB07.Dat) throw Loi("UPM_B07_XUNG_DOT", kqB07.LyDo);

        var coNgoaiTruong = ids.Any(x => gvs[x].LoaiGV == LoaiGiangVien.NgoaiTruong);
        var coCoHuu = ids.Any(x => gvs[x].LoaiGV == LoaiGiangVien.CoHuu);
        var kqB11 = DeTaiRules.KiemTraGVNgoaiTruong(coNgoaiTruong, coCoHuu);
        if (!kqB11.Dat) throw Loi("UPM_B11_NGOAI_TRUONG_CAN_CO_HUU", kqB11.LyDo);

        var tai = await TaiTheoGiangVienAsync(dotId, ids.Distinct().ToArray(), svTrongDotId, ct);
        var vuotQuota = new HashSet<long>();
        foreach (var id in ids.Distinct())
        {
            var gv = gvs[id];
            var dangCo = tai.GetValueOrDefault(id);
            var kq = DeTaiRules.KiemTraQuota(dangCo, gv.QuotaToiDa, choPhepVuotQuota);
            if (!kq.Dat) throw Loi("UPM_B03_VUOT_QUOTA", $"{gv.MaGV} {kq.LyDo}");
            if (dangCo + 1 > gv.QuotaToiDa) vuotQuota.Add(id);
        }
        return vuotQuota;
    }

    private void ThemPhanCong(long svTrongDotId, long gvId, VaiTroPhanCong vaiTro, GiangVien gv, DateOnly batDau,
        long? quyetDinhId, string? lyDo, HashSet<long> vuotQuota)
    {
        db.PhanCong.Add(new PhanCong
        {
            SVTrongDotId = svTrongDotId,
            GiangVienId = gvId,
            VaiTro = vaiTro,
            NgayBatDau = batDau,
            QuyetDinhId = quyetDinhId,
            LyDoThayDoi = lyDo,
            // Đồng HD ngoài trường chỉ nhận xét, điểm do GV cơ hữu nhập (O8).
            DuocNhapDiem = vaiTro != VaiTroPhanCong.DongHD && gv.LoaiGV == LoaiGiangVien.CoHuu,
            TuKhaiXungDot = false,
            VuotQuotaDuyetBoiId = vuotQuota.Contains(gvId) ? currentUser.TaiKhoanId : null,
        });
    }

    private async Task<Dictionary<long, GiangVien>> LayGiangVienAsync(IEnumerable<long> ids, CancellationToken ct)
    {
        var arr = ids.Distinct().ToArray();
        var gvs = await db.GiangVien.AsNoTracking().Where(x => arr.Contains(x.Id) && x.ConHoatDong).ToDictionaryAsync(x => x.Id, ct);
        if (gvs.Count != arr.Length) throw Loi("UPM_GV_NOT_FOUND", "Có giảng viên không tồn tại hoặc đã ngừng hoạt động.");
        return gvs;
    }

    /// <summary>Số SV đang có GV này (GVHD/GVĐD/đồng HD còn hiệu lực) trong đợt – dùng cho quota (B03).</summary>
    private async Task<Dictionary<long, int>> TaiTheoGiangVienAsync(long dotId, long[] gvIds, long? boQuaSVTrongDot, CancellationToken ct)
    {
        var rows = await db.PhanCong.AsNoTracking()
            .Where(p => p.NgayKetThuc == null && p.SVTrongDot.DotId == dotId && gvIds.Contains(p.GiangVienId)
                && (boQuaSVTrongDot == null || p.SVTrongDotId != boQuaSVTrongDot))
            .Select(p => new { p.GiangVienId, p.SVTrongDotId }).ToListAsync(ct);
        return rows.GroupBy(x => x.GiangVienId).ToDictionary(g => g.Key, g => g.Select(x => x.SVTrongDotId).Distinct().Count());
    }

    private async Task<int> SoSVDangCoAsync(long deTaiId, long? boQuaHoSo, CancellationToken ct) =>
        await db.SVTrongDot.CountAsync(x => x.DeTaiId == deTaiId && (boQuaHoSo == null || x.Id != boQuaHoSo), ct);

    /// <summary>
    /// Khoa mà Trưởng BM được quản lý (null = toàn hệ thống). Hồ sơ SV không có cột bộ môn nên BM thấy SV theo khoa
    /// (đúng phạm vi khoa của Phần 3; phân công theo bộ môn qua đề tài / GV).
    /// </summary>
    private async Task<long[]?> KhoaBmAsync(CancellationToken ct)
    {
        var pv = (await quyen.LayAsync(ct)).PhamVi([VaiTroMa.TruongBoMon]);
        if (pv.ToanHeThong) return null;
        // Vai trò gắn bộ môn không mang KhoaId trong PhamViTruyCap → lấy khoa của các bộ môn đó.
        var boMonIds = pv.BoMonIds.ToArray();
        var khoaTuBoMon = await db.BoMon.AsNoTracking().Where(b => boMonIds.Contains(b.Id)).Select(b => b.KhoaId).ToArrayAsync(ct);
        return pv.KhoaIds.Concat(khoaTuBoMon).Distinct().ToArray();
    }

    private async Task<SVTrongDot> HoSoBoMonAsync(long id, CancellationToken ct)
    {
        var khoa = await KhoaBmAsync(ct);
        return await db.SVTrongDot.Where(x => x.Id == id && (khoa == null || khoa.Contains(x.CTDT.KhoaId))).SingleOrDefaultAsync(ct)
            ?? throw Loi("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ trong phạm vi bộ môn của bạn.");
    }

    private async Task<SVTrongDot> LayHoSoCuaSinhVienAsync(long id, long sinhVienId, CancellationToken ct, bool includeDot = false)
    {
        var query = db.SVTrongDot.Where(x => x.Id == id && x.SinhVienId == sinhVienId);
        if (includeDot) query = query.Include(x => x.Dot);
        return await query.SingleOrDefaultAsync(ct)
            ?? throw Loi("UPM_DE_TAI_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ của sinh viên hiện tại.");
    }

    private async Task<IReadOnlyList<PhanCongHoSoItem>> HoSoPhanCongAsync(IQueryable<SVTrongDot> query, CancellationToken ct)
    {
        var rows = await query.AsNoTracking().OrderBy(x => x.SinhVien.MaSV).ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.SinhVienId,
                MaSV = x.SinhVien.MaSV,
                x.SinhVien.HoTen,
                MaCTDT = x.CTDT.MaCTDT,
                x.TrangThai,
                x.DeTaiId,
                TenDeTai = x.DeTai != null ? x.DeTai.TenDeTai : null,
                x.RowVersion,
            }).ToListAsync(ct);
        var ids = rows.Select(x => x.Id).ToArray();
        var pcs = await db.PhanCong.AsNoTracking()
            .Where(p => p.NgayKetThuc == null && ids.Contains(p.SVTrongDotId))
            .Select(p => new { p.SVTrongDotId, p.VaiTro, p.GiangVienId, p.GiangVien.MaGV, p.GiangVien.HoTen, p.GiangVien.LoaiGV, p.Id })
            .OrderBy(p => p.Id)
            .ToListAsync(ct);
        var nhom = pcs.GroupBy(x => x.SVTrongDotId).ToDictionary(g => g.Key, g => g.ToList());
        GiangVienTomTat? Lay(long hoSoId, VaiTroPhanCong vt) =>
            nhom.TryGetValue(hoSoId, out var list) && list.FirstOrDefault(x => x.VaiTro == vt) is { } p
                ? new GiangVienTomTat(p.GiangVienId, p.MaGV, p.HoTen, p.LoaiGV) : null;

        return rows.Select(x => new PhanCongHoSoItem(
            x.Id, x.SinhVienId, x.MaSV, x.HoTen, x.MaCTDT, x.TrangThai, x.DeTaiId, x.TenDeTai,
            Lay(x.Id, VaiTroPhanCong.GVHD), Lay(x.Id, VaiTroPhanCong.GVDD),
            nhom.TryGetValue(x.Id, out var l)
                ? l.Where(p => p.VaiTro == VaiTroPhanCong.DongHD).Select(p => new GiangVienTomTat(p.GiangVienId, p.MaGV, p.HoTen, p.LoaiGV)).ToArray()
                : [],
            Convert.ToBase64String(x.RowVersion))).ToArray();
    }

    /// <summary>Đề tài kèm số SV đã nhận (SVTrongDots) và mức hiển thị – một truy vấn.</summary>
    private static async Task<IReadOnlyList<DeTaiItem>> ProjectAsync(IQueryable<DeTai> query, CancellationToken ct)
    {
        var rows = await query.OrderBy(x => x.TenDeTai).Select(x => new
        {
            x.Id, x.DotId, MaDot = x.Dot.MaDot, x.BoMonId, MaBoMon = x.BoMon.MaBM, TenBoMon = x.BoMon.TenBM,
            x.TenDeTai, x.MoTa, x.LoaiDeTai, x.MaxSV, x.NguonDeXuat, x.GVDeXuatId, TenGVDeXuat = x.GVDeXuat!.HoTen,
            x.SVDeXuatId, MaSVDeXuat = x.SVDeXuat!.MaSV, x.TrangThaiDuyet, x.CanhBaoTrung,
            SoSVDaNhan = x.SVTrongDots.Count(),
        }).ToListAsync(ct);
        return rows.Select(x => new DeTaiItem(x.Id, x.DotId, x.MaDot, x.BoMonId, x.MaBoMon, x.TenBoMon, x.TenDeTai, x.MoTa,
            x.LoaiDeTai, x.MaxSV, x.NguonDeXuat, x.GVDeXuatId, x.TenGVDeXuat, x.SVDeXuatId, x.MaSVDeXuat, x.TrangThaiDuyet,
            x.CanhBaoTrung, x.SoSVDaNhan, x.SoSVDaNhan >= x.MaxSV, [])).ToArray();
    }

    private async Task<DeTaiItem> ItemAsync(long id, IReadOnlyList<DeTaiTrungItem>? trung, CancellationToken ct)
    {
        var item = (await ProjectAsync(db.DeTai.AsNoTracking().Where(x => x.Id == id), ct)).Single();
        return trung is null ? item : item with { TrungVoi = trung };
    }

    private async Task<(bool CanhBao, IReadOnlyList<DeTaiTrungItem> Trung)> KiemTraTrungAsync(DotDATN dot, string ten, string? moTa, CancellationToken ct)
    {
        var khac = await db.DeTai.AsNoTracking().Where(x => x.Dot.CoSoId == dot.CoSoId && x.DotId != dot.Id)
            .Select(x => new { x.Id, x.TenDeTai, x.MoTa, x.Dot.MaDot, x.Dot.NamHoc }).ToListAsync(ct);
        var map = khac.ToDictionary(x => x.Id);
        var trung = DeTaiRules.TimTrung(ten, moTa, khac.Select(x => (x.Id, x.TenDeTai, x.MoTa)))
            .Select(t => new DeTaiTrungItem(t.Id, t.TenDeTai, map[t.Id].MaDot, map[t.Id].NamHoc, t.DoTuongDong, t.TrungTen))
            .ToArray();
        return (trung.Length > 0, trung);
    }

    private async Task KiemTraBoMonAsync(long boMonId, long coSoId, CancellationToken ct)
    {
        if (!await db.BoMon.AnyAsync(x => x.Id == boMonId && x.Khoa.CoSoId == coSoId, ct))
            throw Loi("UPM_DE_TAI_BO_MON_KHONG_HOP_LE", "Bộ môn không thuộc cơ sở của đợt.");
    }

    private static void KiemTraSoSV(LoaiDeTai loai, int maxSV)
    {
        var kq = DeTaiRules.KiemTraSoSV(loai, maxSV);
        if (!kq.Dat)
            throw Loi(kq.MaNgoaiLe == "B01" ? "UPM_B01_SO_SV_KHONG_HOP_LE" : "UPM_B02_NHOM_KHONG_HOP_LE", kq.LyDo);
    }

    private static string KiemTraTen(string? ten)
    {
        var t = (ten ?? string.Empty).Trim();
        if (t.Length is 0 or > 500) throw Loi("UPM_DE_TAI_TEN_KHONG_HOP_LE", "Tên đề tài từ 1 đến 500 ký tự.");
        return t;
    }

    private static string? KiemTraMoTa(string? moTa)
    {
        var m = string.IsNullOrWhiteSpace(moTa) ? null : moTa.Trim();
        if (m?.Length > 4000) throw Loi("UPM_DE_TAI_MO_TA_QUA_DAI", "Mô tả tối đa 4.000 ký tự.");
        return m;
    }

    private async Task<DotDATN> DotMoAsync(long dotId, CancellationToken ct)
    {
        // Đợt thuộc cơ sở người dùng được phép (GV/BM có phạm vi khoa/BM nhưng vẫn thấy đợt của cơ sở đó).
        var dot = await db.DotDATN.AsNoTracking().SingleOrDefaultAsync(x => x.Id == dotId, ct)
            ?? throw Loi("UPM_DOT_NOT_FOUND", "Không tìm thấy đợt trong phạm vi được phép.");
        if (!(await quyen.LayAsync(ct)).CoSoDuocPhep.Contains(dot.CoSoId))
            throw Loi("UPM_DOT_NOT_FOUND", "Không tìm thấy đợt trong phạm vi được phép.");
        if (dot.TrangThai == TrangThaiDot.Dong) throw Loi("UPM_DOT_DA_DONG", "Đợt đã đóng.");
        return dot;
    }

    private async Task<DeTai> KiemTraDeTaiCuaGVAsync(long deTaiId, long gv, CancellationToken ct) =>
        await db.DeTai.SingleOrDefaultAsync(x => x.Id == deTaiId && x.GVDeXuatId == gv && x.TrangThaiDuyet == TrangThaiDuyetDeTai.DaDuyet, ct)
        ?? throw Loi("UPM_DE_TAI_KHONG_THUOC_GV", "Đề tài không tồn tại, chưa được duyệt, hoặc không phải của bạn.");

    private async Task KiemTraQuyenAsync(string quyenCan, CancellationToken ct)
    {
        var current = await quyen.LayAsync(ct);
        if (!current.CoQuyen(quyenCan)) throw Loi("UPM_PERMISSION_DENIED", "Bạn không có quyền thực hiện thao tác này.");
    }

    private long GiangVienHienTai() =>
        currentUser.GiangVienId ?? throw Loi("UPM_CHI_GIANG_VIEN", "Chức năng này chỉ dành cho giảng viên.");

    private long SinhVienHienTai() =>
        currentUser.SinhVienId ?? throw Loi("UPM_CHI_SINH_VIEN", "Chức năng này chỉ dành cho sinh viên.");

    private static string Cat(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";

    private static DomainException Loi(string code, string message) => new(code, message);
}
