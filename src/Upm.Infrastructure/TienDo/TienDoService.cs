using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Upm.Application.Abstractions;
using Upm.Application.PhanQuyen;
using Upm.Application.TienDo;
using Upm.Application.Workflow;
using Upm.Domain.Common;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Domain.PhanQuyen;
using Upm.Domain.Rules;
using Upm.Domain.Workflow;
using Upm.Infrastructure.Persistence;
using Upm.Infrastructure.Workflow;

namespace Upm.Infrastructure.TienDo;

/// <summary>
/// UC-3a: mốc tiến độ, phiếu giao nhiệm vụ (theo từng SV), báo cáo 3 mốc, nhận xét GVHD, đơn gia hạn / xin dừng,
/// tạm dừng (T05), thông báo trong ứng dụng. Quy tắc thuần ở <see cref="TienDoRules"/>.
/// </summary>
public sealed class TienDoService(
    UpmDbContext db,
    IQuyenHienTai quyen,
    ICurrentUser currentUser,
    IClock clock,
    IWorkflowService workflow,
    IFileStorage files,
    IOptions<TepOptions> tepOptions) : ITienDoService
{
    private static readonly string[] DinhDangTepCho = [".pdf", ".docx"];

    // ------------------------------------------------------------------ Mốc tiến độ

    public async Task<IReadOnlyList<MocTienDoItem>> DanhSachMocAsync(long dotId, CancellationToken ct = default)
    {
        await KiemTraTruyCapDotAsync(dotId, ct);
        var mocs = await LayMocAsync(dotId, ct);
        return mocs.Select(ToMocItem).ToArray();
    }

    public async Task<MocTienDoItem> CapNhatMocAsync(long mocId, CapNhatMocRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var q = await quyen.LayAsync(ct);
        if (!q.CoQuyen(Quyen.DotQuanLy) && !q.CoQuyen(Quyen.PhanCongQuanLy))
            throw Loi("UPM_PERMISSION_DENIED", "Chỉ Phòng Đào tạo hoặc Trưởng Bộ môn được sửa mốc tiến độ.");
        var moc = await db.MocTienDo.Include(x => x.Dot).SingleOrDefaultAsync(x => x.Id == mocId, ct)
            ?? throw Loi("UPM_MOC_NOT_FOUND", "Không tìm thấy mốc tiến độ.");
        if (!q.CoSoDuocPhep.Contains(moc.Dot.CoSoId)) throw Loi("UPM_MOC_NOT_FOUND", "Không tìm thấy mốc trong phạm vi được phép.");
        if (moc.Dot.TrangThai == TrangThaiDot.Dong) throw Loi("UPM_DOT_DA_DONG", "Đợt đã đóng, không sửa mốc.");
        if (request.NgayHan < moc.Dot.NgayBatDau) throw Loi("UPM_MOC_HAN_KHONG_HOP_LE", "Hạn mốc không được trước ngày bắt đầu đợt.");
        if (moc.MaMoc == MaMocTienDo.M2 && request.YeuCauToiThieuPct is { } pct && pct is < 60m or > 70m)
            throw Loi("UPM_MOC_YEU_CAU_KHONG_HOP_LE", "Mốc M2 yêu cầu tối thiểu từ 60% đến 70%.");
        if (moc.MaMoc != MaMocTienDo.M2 && request.YeuCauToiThieuPct is not null)
            throw Loi("UPM_MOC_YEU_CAU_KHONG_HOP_LE", "Chỉ mốc M2 có yêu cầu phần trăm tối thiểu.");
        moc.NgayHan = request.NgayHan;
        moc.YeuCauToiThieuPct = request.YeuCauToiThieuPct;
        moc.MoTa = string.IsNullOrWhiteSpace(request.MoTa) ? null : request.MoTa.Trim();
        await db.SaveChangesAsync(ct);
        return ToMocItem(moc);
    }

    // ------------------------------------------------------------------ Phiếu giao nhiệm vụ

    public async Task<PhieuGiaoNhiemVuItem?> LayPhieuAsync(long svTrongDotId, CancellationToken ct = default)
    {
        var hoSo = await KiemTraXemHoSoAsync(svTrongDotId, ct);
        var phieu = await db.PhieuGiaoNhiemVu.AsNoTracking().SingleOrDefaultAsync(x => x.SVTrongDotId == hoSo.Id, ct);
        return phieu is null ? null : await ToPhieuAsync(phieu, hoSo.Id, ct);
    }

    /// <summary>GVHD (còn hiệu lực) lập phiếu cho TỪNG hồ sơ – nhóm nhiều SV có nhiều phiếu (B02, đặc tả mục 5).</summary>
    public async Task<PhieuGiaoNhiemVuItem> LuuPhieuAsync(long svTrongDotId, PhieuGiaoNhiemVuRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var hoSo = await LayHoSoAsync(svTrongDotId, ct);
        await KiemTraLaGvhdAsync(hoSo.Id, ct);
        var noiDung = (request.NoiDungNhiemVu ?? string.Empty).Trim();
        if (noiDung.Length is 0 or > 4000) throw Loi("UPM_PHIEU_NOI_DUNG_KHONG_HOP_LE", "Nội dung nhiệm vụ từ 1 đến 4.000 ký tự.");
        var yeuCau = string.IsNullOrWhiteSpace(request.YeuCauSanPham) ? null : request.YeuCauSanPham.Trim();
        if (yeuCau?.Length > 4000) throw Loi("UPM_PHIEU_NOI_DUNG_KHONG_HOP_LE", "Yêu cầu sản phẩm tối đa 4.000 ký tự.");

        var phieu = await db.PhieuGiaoNhiemVu.SingleOrDefaultAsync(x => x.SVTrongDotId == hoSo.Id, ct);
        if (phieu is null)
        {
            phieu = new PhieuGiaoNhiemVu { SVTrongDotId = hoSo.Id };
            db.PhieuGiaoNhiemVu.Add(phieu);
        }
        phieu.NoiDungNhiemVu = noiDung;
        phieu.YeuCauSanPham = yeuCau;
        phieu.NgayGiao = request.NgayGiao;
        await db.SaveChangesAsync(ct);
        return await ToPhieuAsync(phieu, hoSo.Id, ct);
    }

    public async Task<(byte[] Noi, string TenFile)> XuatPhieuPdfAsync(long svTrongDotId, CancellationToken ct = default)
    {
        var item = await LayPhieuAsync(svTrongDotId, ct)
            ?? throw Loi("UPM_PHIEU_CHUA_LAP", "Chưa lập phiếu giao nhiệm vụ cho hồ sơ này.");
        var dot = await db.SVTrongDot.AsNoTracking().Where(x => x.Id == svTrongDotId)
            .Select(x => new { x.Dot.MaDot, x.Dot.NamHoc, DotTen = x.Dot.TenDot, x.CTDT.TenCTDT }).SingleAsync(ct);
        var gvhd = await db.PhanCong.AsNoTracking().Where(x => x.SVTrongDotId == svTrongDotId && x.VaiTro == VaiTroPhanCong.GVHD && x.NgayKetThuc == null)
            .Select(x => x.GiangVien.HoTen).FirstOrDefaultAsync(ct) ?? string.Empty;
        var noi = PhieuGiaoPdfBuilder.Dung(new PhieuGiaoPdfDuLieu(
            item.MaSV, item.HoTen, dot.TenCTDT, dot.DotTen, dot.NamHoc, gvhd, item.NoiDungNhiemVu, item.YeuCauSanPham, item.NgayGiao));
        return (noi, $"phieu-giao-nhiem-vu-{item.MaSV}.pdf");
    }

    // ------------------------------------------------------------------ Báo cáo tiến độ

    public async Task<BaoCaoTienDoItem> NopBaoCaoAsync(long svTrongDotId, long mocId, Stream file, string tenFile, string contentType,
        NopBaoCaoRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(file);
        var hoSo = await LayHoSoCuaSinhVienAsync(svTrongDotId, ct);
        var kqAI = TienDoRules.KiemTraCamKetAI(request.CamKetAI);
        if (!kqAI.Dat) throw Loi("UPM_C07_CAM_KET_BAT_BUOC", kqAI.LyDo);
        if (request.TyLeHoanThanhPct is < 0 or > 100) throw Loi("UPM_BAO_CAO_TY_LE_KHONG_HOP_LE", "Tỉ lệ hoàn thành từ 0 đến 100%.");
        if (hoSo.TrangThai != TrangThaiSVTrongDot.DANG_THUC_HIEN)
            throw Loi("UPM_BAO_CAO_TRANG_THAI", "Chỉ nộp báo cáo khi hồ sơ đang thực hiện.");
        var moc = await db.MocTienDo.AsNoTracking().SingleOrDefaultAsync(x => x.Id == mocId && x.DotId == hoSo.DotId, ct)
            ?? throw Loi("UPM_MOC_NOT_FOUND", "Mốc không thuộc đợt của hồ sơ.");
        var dot = await db.DotDATN.AsNoTracking().SingleAsync(x => x.Id == hoSo.DotId, ct);
        if (dot.TrangThai == TrangThaiDot.Dong) throw Loi("UPM_DOT_DA_DONG", "Đợt đã đóng.");

        var tep = await LuuTepAsync(file, tenFile, contentType, ct);
        var now = clock.Now.UtcDateTime;
        var yeuCau = TienDoRules.YeuCauToiThieu(moc.MaMoc, moc.YeuCauToiThieuPct);
        var bao = new BaoCaoTienDo
        {
            SVTrongDotId = hoSo.Id,
            MocTienDoId = moc.Id,
            NgayNop = now,
            FileId = tep.Id,
            TyLeHoanThanhPct = request.TyLeHoanThanhPct,
            KetQua = TienDoRules.PhanLoai(request.TyLeHoanThanhPct, yeuCau),
            CamKetAI = true,
            ThoiDiemCamKetAI = now,
        };
        db.BaoCaoTienDo.Add(bao);
        await db.SaveChangesAsync(ct);
        return ToBaoCao(bao, tep.TenGoc);
    }

    public async Task<BaoCaoTienDoItem> NhanXetBaoCaoAsync(long baoCaoId, NhanXetBaoCaoRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var bao = await db.BaoCaoTienDo.Include(x => x.SVTrongDot).SingleOrDefaultAsync(x => x.Id == baoCaoId, ct)
            ?? throw Loi("UPM_BAO_CAO_NOT_FOUND", "Không tìm thấy báo cáo.");
        await KiemTraLaGvhdAsync(bao.SVTrongDotId, ct);
        if (request.DiemTienDo is { } d && d is < 0 or > 10)
            throw Loi("UPM_DIEM_NGOAI_THANG_10", "Điểm tiến độ phải từ 0 đến 10.");
        var nx = string.IsNullOrWhiteSpace(request.NhanXet) ? null : request.NhanXet.Trim();
        if (nx?.Length > 1000) throw Loi("UPM_NHAN_XET_QUA_DAI", "Nhận xét tối đa 1.000 ký tự.");
        bao.DiemTienDo = request.DiemTienDo is null ? null : Math.Round(request.DiemTienDo.Value, 1, MidpointRounding.AwayFromZero);
        bao.GVHDNhanXet = nx;
        await db.SaveChangesAsync(ct);
        var ten = bao.FileId is { } f ? await db.TepDinhKem.AsNoTracking().Where(t => t.Id == f).Select(t => t.TenGoc).FirstOrDefaultAsync(ct) : null;
        return ToBaoCao(bao, ten);
    }

    // ------------------------------------------------------------------ Danh sách theo vai trò

    public async Task<IReadOnlyList<HoSoTienDoItem>> DanhSachGvAsync(long dotId, CancellationToken ct = default)
    {
        var gv = GiangVienHienTai();
        var ids = await db.PhanCong.AsNoTracking()
            .Where(p => p.GiangVienId == gv && p.VaiTro == VaiTroPhanCong.GVHD && p.NgayKetThuc == null && p.SVTrongDot.DotId == dotId)
            .Select(p => p.SVTrongDotId).ToListAsync(ct);
        return await HoSoTienDoAsync(dotId, db.SVTrongDot.AsNoTracking().Where(x => ids.Contains(x.Id)), ct);
    }

    public async Task<IReadOnlyList<HoSoTienDoItem>> DanhSachBoMonAsync(long dotId, CancellationToken ct = default)
    {
        await KiemTraBoMonAsync(ct);
        var khoa = await KhoaBmAsync(ct);
        return await HoSoTienDoAsync(dotId, db.SVTrongDot.AsNoTracking().Where(x => x.DotId == dotId
            && (khoa == null || khoa.Contains(x.CTDT.KhoaId))), ct);
    }

    public async Task<IReadOnlyList<SvCham>> SvChamAsync(long dotId, CancellationToken ct = default)
    {
        var ds = await DanhSachBoMonAsync(dotId, ct);
        return ds.Select(h =>
        {
            var canh = h.CacMoc.Count(m => m.TrangThai == KetQuaTienDo.CanhBao);
            var khong = h.CacMoc.Count(m => m.TrangThai == KetQuaTienDo.KhongNop);
            return new SvCham(h.SVTrongDotId, h.MaSV, h.HoTen, h.TenGVHD, canh, khong, h.CanNhacRut);
        }).Where(x => x.SoCanhBao + x.SoKhongNop > 0).ToArray();
    }

    public async Task<IReadOnlyList<HoSoTienDoItem>> TienDoCuaToiAsync(long dotId, CancellationToken ct = default)
    {
        var sv = SinhVienHienTai();
        return await HoSoTienDoAsync(dotId, db.SVTrongDot.AsNoTracking().Where(x => x.DotId == dotId && x.SinhVienId == sv), ct);
    }

    // ------------------------------------------------------------------ Đơn: gia hạn (C05), xin dừng (C04)

    public async Task<DonTienDoItem> TaoDonAsync(long svTrongDotId, DonTienDoTaoRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.LoaiDon is not (LoaiDon.GiaHan or LoaiDon.XinDung))
            throw Loi("UPM_DON_LOAI_KHONG_HOP_LE", "Chỉ hỗ trợ đơn gia hạn (GiaHan) hoặc xin dừng (XinDung).");
        var noiDung = (request.NoiDung ?? string.Empty).Trim();
        if (noiDung.Length is 0 or > 4000) throw Loi("UPM_DON_NOI_DUNG_KHONG_HOP_LE", "Nội dung đơn từ 1 đến 4.000 ký tự.");
        var hoSo = await LayHoSoCuaSinhVienAsync(svTrongDotId, ct);
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
        return new DonTienDoItem(don.Id, don.LoaiDon, hoSo.Id, string.Empty, string.Empty, don.NoiDung, don.NgayGui,
            don.TrangThai, null, null);
    }

    public async Task<IReadOnlyList<DonTienDoItem>> DanhSachDonAsync(long dotId, CancellationToken ct = default)
    {
        var q = await quyen.LayAsync(ct);
        if (!q.CoQuyen(Quyen.PhanCongQuanLy) && currentUser.GiangVienId is null)
            throw Loi("UPM_PERMISSION_DENIED", "Không có quyền xem đơn tiến độ.");
        var query = db.DonTu.AsNoTracking().Where(x => x.SVTrongDot.DotId == dotId
            && (x.LoaiDon == LoaiDon.GiaHan || x.LoaiDon == LoaiDon.XinDung));
        if (currentUser.GiangVienId is { } gv && !q.CoQuyen(Quyen.PhanCongQuanLy))
            query = query.Where(x => x.SVTrongDot.PhanCongs.Any(p => p.GiangVienId == gv && p.VaiTro == VaiTroPhanCong.GVHD && p.NgayKetThuc == null));
        else
        {
            var khoa = await KhoaBmAsync(ct);
            if (khoa is not null) query = query.Where(x => khoa.Contains(x.SVTrongDot.CTDT.KhoaId));
        }
        return await query.OrderByDescending(x => x.NgayGui).ThenByDescending(x => x.Id)
            .Select(x => new DonTienDoItem(x.Id, x.LoaiDon, x.SVTrongDotId, x.SVTrongDot.SinhVien.MaSV, x.SVTrongDot.SinhVien.HoTen,
                x.NoiDung, x.NgayGui, x.TrangThai, x.KetQuaXuLy, x.HanMoi)).ToListAsync(ct);
    }

    /// <summary>
    /// Gia hạn: GVHD hoặc Trưởng BM duyệt, hạn mới phải sau hạn cũ (C05). Xin dừng: Trưởng BM duyệt → T06 RUT (C04)
    /// qua máy trạng thái, bắt buộc lý do. Không tự chuyển trạng thái nếu đơn bị từ chối.
    /// </summary>
    public async Task DuyetDonAsync(long donTuId, DuyetDonTienDoRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var don = await db.DonTu.Include(x => x.SVTrongDot).SingleOrDefaultAsync(x => x.Id == donTuId
            && (x.LoaiDon == LoaiDon.GiaHan || x.LoaiDon == LoaiDon.XinDung), ct)
            ?? throw Loi("UPM_DON_NOT_FOUND", "Không tìm thấy đơn tiến độ.");
        if (don.TrangThai != TrangThaiDonTu.ChoXuLy) throw Loi("UPM_DON_DA_XU_LY", "Đơn đã được xử lý.");
        var q = await quyen.LayAsync(ct);
        var laBm = q.CoQuyen(Quyen.PhanCongQuanLy);
        if (don.LoaiDon == LoaiDon.XinDung && !laBm)
            throw Loi("UPM_PERMISSION_DENIED", "Chỉ Trưởng Bộ môn duyệt đơn xin dừng.");
        if (don.LoaiDon == LoaiDon.GiaHan && !laBm) await KiemTraLaGvhdAsync(don.SVTrongDotId, ct);

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

        if (don.LoaiDon == LoaiDon.GiaHan)
        {
            var hanCu = await HanHienTaiCuaMocChuaNopAsync(don.SVTrongDot, ct);
            var hanMoi = request.HanMoi ?? throw Loi("UPM_GIA_HAN_THIEU_HAN", "Cần nhập hạn mới khi gia hạn.");
            var kq = TienDoRules.KiemTraHanGiaHan(hanCu, hanMoi);
            if (!kq.Dat) throw Loi("UPM_C05_HAN_MOI_KHONG_HOP_LE", kq.LyDo);
            don.HanMoi = hanMoi;
            don.KetQuaXuLy = $"Đã gia hạn đến {hanMoi:dd/MM/yyyy}.";
        }
        else
        {
            var lyDo = string.IsNullOrWhiteSpace(request.LyDo) ? don.NoiDung : request.LyDo.Trim();
            don.KetQuaXuLy = "Đã duyệt xin dừng; chuyển trạng thái RUT.";
            await db.SaveChangesAsync(ct);
            // C04: dừng do đơn đã duyệt – chuyển qua máy trạng thái (T06 cho phép Trưởng BM), có lý do.
            var hoSo = await db.SVTrongDot.SingleAsync(x => x.Id == don.SVTrongDotId, ct);
            don.TrangThai = TrangThaiDonTu.DaDuyet;
            don.NguoiXuLyId = currentUser.TaiKhoanId;
            don.NgayXuLy = now;
            await db.SaveChangesAsync(ct);
            await workflow.TransitionAsync(hoSo.Id, new WorkflowTransitionRequest(Convert.ToBase64String(hoSo.RowVersion),
                MaChuyen: "T06", LyDo: Cat(lyDo, 500)), ct);
            await tx.CommitAsync(ct);
            return;
        }

        don.TrangThai = TrangThaiDonTu.DaDuyet;
        don.NguoiXuLyId = currentUser.TaiKhoanId;
        don.NgayXuLy = now;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <summary>C03: nghỉ học tạm thời → Phòng ĐT chuyển TAM_DUNG (T05) qua máy trạng thái (điều kiện: SV nghỉ tạm thời).</summary>
    public async Task TamDungAsync(long svTrongDotId, string lyDo, CancellationToken ct = default)
    {
        var q = await quyen.LayAsync(ct);
        if (!q.CoQuyen(Quyen.DotQuanLy)) throw Loi("UPM_PERMISSION_DENIED", "Chỉ Phòng Đào tạo chuyển tạm dừng.");
        var hoSo = await db.SVTrongDot.SingleOrDefaultAsync(x => x.Id == svTrongDotId, ct)
            ?? throw Loi("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ.");
        if (!q.CoSoDuocPhep.Contains(hoSo.CoSoId)) throw Loi("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ trong phạm vi được phép.");
        var ly = string.IsNullOrWhiteSpace(lyDo) ? "Nghỉ học tạm thời (C03)." : lyDo.Trim();
        await workflow.TransitionAsync(hoSo.Id, new WorkflowTransitionRequest(Convert.ToBase64String(hoSo.RowVersion),
            MaChuyen: "T05", LyDo: Cat(ly, 500)), ct);
    }

    // ------------------------------------------------------------------ Thông báo

    public async Task<IReadOnlyList<ThongBaoItem>> ThongBaoCuaToiAsync(CancellationToken ct = default)
    {
        var tk = currentUser.TaiKhoanId ?? throw Loi("UPM_CHUA_DANG_NHAP", "Cần đăng nhập.");
        return await db.ThongBao.AsNoTracking().Where(x => x.TaiKhoanId == tk)
            .OrderByDescending(x => x.NgayTao).ThenByDescending(x => x.Id).Take(100)
            .Select(x => new ThongBaoItem(x.Id, x.TieuDe, x.NoiDung, x.DaDoc, x.NgayTao)).ToListAsync(ct);
    }

    public async Task DanhDauDocAsync(long thongBaoId, CancellationToken ct = default)
    {
        var tk = currentUser.TaiKhoanId ?? throw Loi("UPM_CHUA_DANG_NHAP", "Cần đăng nhập.");
        var tb = await db.ThongBao.SingleOrDefaultAsync(x => x.Id == thongBaoId && x.TaiKhoanId == tk, ct)
            ?? throw Loi("UPM_THONG_BAO_NOT_FOUND", "Không tìm thấy thông báo.");
        tb.DaDoc = true;
        await db.SaveChangesAsync(ct);
    }

    // ================================================================== Lõi tính toán (dùng chung với job)

    /// <summary>Tính hạn hiệu lực từng mốc: gia hạn đã duyệt (HanMoi) áp cho MỐC CHƯA NỘP ĐẦU TIÊN (C05).</summary>
    internal static IReadOnlyList<(MocRow Moc, DateOnly Han, bool DaGiaHan)> TinhHan(
        IReadOnlyList<MocRow> cacMoc, IReadOnlyDictionary<long, BaoRow> baoTheoMoc, DateOnly? hanMoi)
    {
        var ketQua = new List<(MocRow, DateOnly, bool)>();
        var daApGiaHan = false;
        foreach (var moc in cacMoc.OrderBy(x => x.MaMoc))
        {
            var han = moc.NgayHan;
            var chuaNop = !baoTheoMoc.ContainsKey(moc.Id);
            if (!daApGiaHan && chuaNop && hanMoi is { } hm && hm > moc.NgayHan)
            {
                han = hm;
                daApGiaHan = true;
                ketQua.Add((moc, han, true));
                continue;
            }
            if (chuaNop) daApGiaHan = true; // một gia hạn chỉ áp cho một mốc
            ketQua.Add((moc, han, false));
        }
        return ketQua;
    }

    private async Task<HoSoTienDoItem[]> HoSoTienDoAsync(long dotId, IQueryable<SVTrongDot> hoSoQuery, CancellationToken ct)
    {
        var today = clock.Today;
        var mocs = (await LayMocAsync(dotId, ct)).Select(m => new MocRow(m.Id, m.MaMoc, m.NgayHan, m.YeuCauToiThieuPct)).ToArray();
        var rows = await hoSoQuery.OrderBy(x => x.SinhVien.MaSV).ThenBy(x => x.Id).Select(x => new
        {
            x.Id,
            x.SinhVienId,
            MaSV = x.SinhVien.MaSV,
            HoTen = x.SinhVien.HoTen,
            x.TrangThai,
            GVHDId = x.PhanCongs.Where(p => p.VaiTro == VaiTroPhanCong.GVHD && p.NgayKetThuc == null).Select(p => (long?)p.GiangVienId).FirstOrDefault(),
            TenGVHD = x.PhanCongs.Where(p => p.VaiTro == VaiTroPhanCong.GVHD && p.NgayKetThuc == null).Select(p => p.GiangVien.HoTen).FirstOrDefault(),
            CoPhieu = db.PhieuGiaoNhiemVu.Any(pg => pg.SVTrongDotId == x.Id),
        }).ToListAsync(ct);
        var ids = rows.Select(r => r.Id).ToArray();

        var baoCao = await db.BaoCaoTienDo.AsNoTracking().Where(b => ids.Contains(b.SVTrongDotId))
            .OrderByDescending(b => b.NgayNop).ThenByDescending(b => b.Id)
            .Select(b => new BaoRow(b.Id, b.SVTrongDotId, b.MocTienDoId, b.NgayNop, b.FileId, b.TyLeHoanThanhPct, b.KetQua, b.DiemTienDo, b.GVHDNhanXet, b.CamKetAI))
            .ToListAsync(ct);
        var fileIds = baoCao.Where(b => b.FileId is not null).Select(b => b.FileId!.Value).Distinct().ToArray();
        var tenFile = await db.TepDinhKem.AsNoTracking().Where(t => fileIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.TenGoc, ct);
        var hanMoi = await db.DonTu.AsNoTracking()
            .Where(d => ids.Contains(d.SVTrongDotId) && d.LoaiDon == LoaiDon.GiaHan && d.TrangThai == TrangThaiDonTu.DaDuyet && d.HanMoi != null)
            .OrderByDescending(d => d.NgayXuLy).ThenByDescending(d => d.Id)
            .Select(d => new { d.SVTrongDotId, d.HanMoi }).ToListAsync(ct);
        var donCho = (await db.DonTu.AsNoTracking().Where(d => ids.Contains(d.SVTrongDotId) && d.TrangThai == TrangThaiDonTu.ChoXuLy)
            .Select(d => d.SVTrongDotId).Distinct().ToListAsync(ct)).ToHashSet();

        var ketQua = new List<HoSoTienDoItem>();
        foreach (var r in rows)
        {
            var baoTheoMoc = baoCao.Where(b => b.SVTrongDotId == r.Id)
                .GroupBy(b => b.MocTienDoId).ToDictionary(g => g.Key, g => g.First());
            var hanGiaHan = hanMoi.FirstOrDefault(x => x.SVTrongDotId == r.Id)?.HanMoi;
            var tinh = TinhHan(mocs, baoTheoMoc.ToDictionary(kv => kv.Key, kv => kv.Value), hanGiaHan);
            var cacMoc = tinh.Select(t =>
            {
                baoTheoMoc.TryGetValue(t.Moc.Id, out var bao);
                var tt = TienDoRules.TrangThaiMoc(bao?.KetQua, today, t.Han);
                var item = bao is null ? null : ToBaoCaoItem(bao, bao.FileId is { } f && tenFile.TryGetValue(f, out var tn) ? tn : null);
                return new TrangThaiMocItem(t.Moc.Id, t.Moc.MaMoc, t.Han, t.DaGiaHan, t.Moc.YeuCauToiThieuPct, item, tt);
            }).ToArray();
            ketQua.Add(new HoSoTienDoItem(r.Id, r.SinhVienId, r.MaSV, r.HoTen, r.TrangThai, r.TenGVHD, r.GVHDId, r.CoPhieu,
                cacMoc, TienDoRules.NenGoiYRut(cacMoc.Select(m => m.TrangThai)), donCho.Contains(r.Id)));
        }
        return ketQua.ToArray();
    }

    internal sealed record MocRow(long Id, MaMocTienDo MaMoc, DateOnly NgayHan, decimal? YeuCauToiThieuPct);

    internal sealed record BaoRow(long Id, long SVTrongDotId, long MocTienDoId, DateTime NgayNop, long? FileId,
        decimal TyLe, KetQuaTienDo KetQua, decimal? Diem, string? NhanXet, bool CamKetAI);

    // ================================================================== Tiện ích truy cập & dữ liệu

    private async Task<List<MocTienDo>> LayMocAsync(long dotId, CancellationToken ct)
    {
        var mocs = await db.MocTienDo.Where(x => x.DotId == dotId).OrderBy(x => x.MaMoc).ToListAsync(ct);
        if (mocs.Count == 0)
        {
            // Đợt tạo trước khi có tính năng mốc: sinh mặc định một lần.
            var dot = await db.DotDATN.AsNoTracking().SingleAsync(x => x.Id == dotId, ct);
            db.MocTienDo.AddRange(DotDATNService.SinhMocMacDinh(dot));
            await db.SaveChangesAsync(ct);
            mocs = await db.MocTienDo.Where(x => x.DotId == dotId).OrderBy(x => x.MaMoc).ToListAsync(ct);
        }
        return mocs;
    }

    private async Task KiemTraTruyCapDotAsync(long dotId, CancellationToken ct)
    {
        if (currentUser.SinhVienId is { } sv)
        {
            if (!await db.SVTrongDot.AnyAsync(x => x.DotId == dotId && x.SinhVienId == sv, ct))
                throw Loi("UPM_DOT_NOT_FOUND", "Bạn chưa có hồ sơ trong đợt này.");
            return;
        }
        var coSo = await db.DotDATN.Where(x => x.Id == dotId).Select(x => (long?)x.CoSoId).SingleOrDefaultAsync(ct)
            ?? throw Loi("UPM_DOT_NOT_FOUND", "Không tìm thấy đợt.");
        if (!(await quyen.LayAsync(ct)).CoSoDuocPhep.Contains(coSo)) throw Loi("UPM_DOT_NOT_FOUND", "Không tìm thấy đợt trong phạm vi được phép.");
    }

    /// <summary>Hồ sơ mà người dùng được xem: SV (của mình), GVHD (còn hiệu lực), hoặc theo cơ sở/khoa được phép.</summary>
    private async Task<SVTrongDot> KiemTraXemHoSoAsync(long svTrongDotId, CancellationToken ct)
    {
        var hoSo = await db.SVTrongDot.AsNoTracking().SingleOrDefaultAsync(x => x.Id == svTrongDotId, ct)
            ?? throw Loi("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ.");
        if (currentUser.SinhVienId == hoSo.SinhVienId) return hoSo;
        if (currentUser.GiangVienId is { } gv
            && await db.PhanCong.AnyAsync(p => p.SVTrongDotId == hoSo.Id && p.GiangVienId == gv && p.NgayKetThuc == null, ct))
            return hoSo;
        var q = await quyen.LayAsync(ct);
        if (q.CoQuyen(Quyen.DotQuanLy) && q.CoSoDuocPhep.Contains(hoSo.CoSoId)) return hoSo;
        throw Loi("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ trong phạm vi được phép.");
    }

    private async Task<SVTrongDot> LayHoSoAsync(long svTrongDotId, CancellationToken ct) =>
        await db.SVTrongDot.SingleOrDefaultAsync(x => x.Id == svTrongDotId, ct)
        ?? throw Loi("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ.");

    private async Task<SVTrongDot> LayHoSoCuaSinhVienAsync(long svTrongDotId, CancellationToken ct)
    {
        var sv = SinhVienHienTai();
        return await db.SVTrongDot.SingleOrDefaultAsync(x => x.Id == svTrongDotId && x.SinhVienId == sv, ct)
            ?? throw Loi("UPM_DOT_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ của sinh viên hiện tại.");
    }

    private async Task KiemTraLaGvhdAsync(long hoSoId, CancellationToken ct)
    {
        var gv = GiangVienHienTai();
        if (!await db.PhanCong.AnyAsync(p => p.SVTrongDotId == hoSoId && p.GiangVienId == gv && p.VaiTro == VaiTroPhanCong.GVHD && p.NgayKetThuc == null, ct))
            throw Loi("UPM_PERMISSION_DENIED", "Chỉ giảng viên hướng dẫn (còn hiệu lực) của hồ sơ này được thực hiện.");
    }

    private async Task KiemTraBoMonAsync(CancellationToken ct)
    {
        var q = await quyen.LayAsync(ct);
        if (!q.CoQuyen(Quyen.PhanCongQuanLy)) throw Loi("UPM_PERMISSION_DENIED", "Chỉ Trưởng Bộ môn xem tổng hợp này.");
    }

    private async Task<long[]?> KhoaBmAsync(CancellationToken ct)
    {
        var pv = (await quyen.LayAsync(ct)).PhamVi([VaiTroMa.TruongBoMon]);
        if (pv.ToanHeThong) return null;
        var boMonIds = pv.BoMonIds.ToArray();
        var khoa = await db.BoMon.AsNoTracking().Where(b => boMonIds.Contains(b.Id)).Select(b => b.KhoaId).ToArrayAsync(ct);
        return pv.KhoaIds.Concat(khoa).Distinct().ToArray();
    }

    private async Task<DateOnly> HanHienTaiCuaMocChuaNopAsync(SVTrongDot hoSo, CancellationToken ct)
    {
        var mocs = (await LayMocAsync(hoSo.DotId, ct)).Select(m => new MocRow(m.Id, m.MaMoc, m.NgayHan, m.YeuCauToiThieuPct)).ToArray();
        var bao = await db.BaoCaoTienDo.AsNoTracking().Where(b => b.SVTrongDotId == hoSo.Id)
            .Select(b => new BaoRow(b.Id, b.SVTrongDotId, b.MocTienDoId, b.NgayNop, b.FileId, b.TyLeHoanThanhPct, b.KetQua, b.DiemTienDo, b.GVHDNhanXet, b.CamKetAI))
            .ToListAsync(ct);
        var theo = bao.GroupBy(b => b.MocTienDoId).ToDictionary(g => g.Key, g => g.First());
        var chuaNop = mocs.Where(m => !theo.ContainsKey(m.Id)).OrderBy(m => m.MaMoc).FirstOrDefault()
            ?? mocs.OrderBy(m => m.MaMoc).Last();
        var tinh = TinhHan(mocs, theo.ToDictionary(kv => kv.Key, kv => kv.Value), null);
        return tinh.Single(t => t.Moc.Id == chuaNop.Id).Han;
    }

    private async Task<LuuTepResult> LuuTepAsync(Stream file, string tenFile, string contentType, CancellationToken ct)
    {
        var ext = Path.GetExtension(tenFile).ToLowerInvariant();
        if (!DinhDangTepCho.Contains(ext)) throw Loi("UPM_TEP_DINH_DANG", "Chỉ nhận file PDF hoặc DOCX.");
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
        return new LuuTepResult(tep.Id, tep.TenGoc);
    }

    private sealed record LuuTepResult(long Id, string TenGoc);

    private async Task<PhieuGiaoNhiemVuItem> ToPhieuAsync(PhieuGiaoNhiemVu phieu, long hoSoId, CancellationToken ct)
    {
        var sv = await db.SVTrongDot.AsNoTracking().Where(x => x.Id == hoSoId)
            .Select(x => new { x.SinhVien.MaSV, x.SinhVien.HoTen }).SingleAsync(ct);
        return new PhieuGiaoNhiemVuItem(phieu.Id, hoSoId, sv.MaSV, sv.HoTen, phieu.NoiDungNhiemVu, phieu.YeuCauSanPham, phieu.NgayGiao);
    }

    private static MocTienDoItem ToMocItem(MocTienDo m) =>
        new(m.Id, m.DotId, m.MaMoc, m.TuanTu, m.TuanDen, m.NgayHan, m.YeuCauToiThieuPct, m.MoTa);

    private static BaoCaoTienDoItem ToBaoCao(BaoCaoTienDo b, string? tenFile) => new(b.Id, b.MocTienDoId, b.NgayNop,
        b.FileId, tenFile, b.TyLeHoanThanhPct, b.KetQua, b.DiemTienDo, b.GVHDNhanXet, b.CamKetAI);

    private static BaoCaoTienDoItem ToBaoCaoItem(BaoRow b, string? tenFile) => new(b.Id, b.MocTienDoId, b.NgayNop,
        b.FileId, tenFile, b.TyLe, b.KetQua, b.Diem, b.NhanXet, b.CamKetAI);

    private long GiangVienHienTai() => currentUser.GiangVienId ?? throw Loi("UPM_CHI_GIANG_VIEN", "Chức năng này chỉ dành cho giảng viên.");

    private long SinhVienHienTai() => currentUser.SinhVienId ?? throw Loi("UPM_CHI_SINH_VIEN", "Chức năng này chỉ dành cho sinh viên.");

    private static string Cat(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";

    private static DomainException Loi(string code, string message) => new(code, message);
}
