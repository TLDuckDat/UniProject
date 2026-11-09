using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upm.Application.TienDo;
using Upm.Application.Workflow;
using Upm.Domain.Common;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Domain.PhanQuyen;
using Upm.Domain.Rules;
using Upm.Domain.Workflow;
using Upm.Infrastructure.Persistence;

namespace Upm.Infrastructure.TienDo;

/// <summary>
/// Job nhắc việc hằng ngày (UC-3a): (1) T04 khi đến ngày bắt đầu đợt; (2) nhắc trước hạn mốc;
/// (3) C01 quá hạn chưa nộp và (4) C02 báo cáo dưới yêu cầu → thông báo trong app + email cho SV, GVHD và Trưởng BM.
/// Chống gửi trùng: mỗi (hồ sơ, loại nhắc, hạn) chỉ có MỘT bản ghi <see cref="LichNhacViec"/> (UK) – bản ghi được
/// tạo trước khi gửi, nên chạy lại trong ngày không gửi lại. Không tự chuyển RUT (C04 chỉ là gợi ý).
/// </summary>
public sealed class NhacViecService(
    UpmDbContext db,
    IWorkflowService workflow,
    IEmailSender email,
    ILogger<NhacViecService> logger) : INhacViecService
{
    public async Task<KetQuaNhacViec> ChayAsync(DateOnly homNay, CancellationToken ct = default)
    {
        var chuyenT04 = await ChuyenT04Async(homNay, ct);
        var soEmail = 0;
        var soThongBao = 0;
        var soBoQua = 0;

        var hoSos = await db.SVTrongDot.AsNoTracking()
            .Where(x => x.Dot.TrangThai != TrangThaiDot.Dong && x.TrangThai == TrangThaiSVTrongDot.DANG_THUC_HIEN)
            .Select(x => new HoSoNhac(x.Id, x.DotId, x.SinhVienId, x.SinhVien.MaSV, x.SinhVien.HoTen, x.SinhVien.Email))
            .ToListAsync(ct);

        foreach (var hs in hoSos)
        {
            var mocs = await db.MocTienDo.AsNoTracking().Where(x => x.DotId == hs.DotId)
                .Select(x => new TienDoService.MocRow(x.Id, x.MaMoc, x.NgayHan, x.YeuCauToiThieuPct)).ToListAsync(ct);
            var baoRows = await db.BaoCaoTienDo.AsNoTracking().Where(x => x.SVTrongDotId == hs.Id)
                .OrderByDescending(x => x.NgayNop).ThenByDescending(x => x.Id)
                .Select(x => new { x.MocTienDoId, x.KetQua }).ToListAsync(ct);
            var baoTheoMoc = baoRows.GroupBy(x => x.MocTienDoId).ToDictionary(g => g.Key, g => g.First().KetQua);
            var hanMoi = await db.DonTu.AsNoTracking().Where(d => d.SVTrongDotId == hs.Id && d.LoaiDon == LoaiDon.GiaHan
                    && d.TrangThai == TrangThaiDonTu.DaDuyet && d.HanMoi != null)
                .OrderByDescending(d => d.NgayXuLy).Select(d => d.HanMoi).FirstOrDefaultAsync(ct);
            var gv = await db.PhanCong.AsNoTracking()
                .Where(p => p.SVTrongDotId == hs.Id && p.VaiTro == VaiTroPhanCong.GVHD && p.NgayKetThuc == null)
                .Select(p => new GvNhac(p.GiangVienId, p.GiangVien.BoMonId, p.GiangVien.HoTen, p.GiangVien.Email))
                .FirstOrDefaultAsync(ct);

            // TinhHan chỉ cần biết mốc nào đã có báo cáo; ghép dòng tối thiểu từ KetQua.
            var baoDuNhan = baoTheoMoc.ToDictionary(kv => kv.Key, kv => new TienDoService.BaoRow(0, hs.Id, kv.Key, default, null, 0, kv.Value, null, null, false));
            var tinh = TienDoService.TinhHan(mocs, baoDuNhan, hanMoi);
            foreach (var (moc, han, _) in tinh)
            {
                var daNop = baoTheoMoc.TryGetValue(moc.Id, out var ketQua);
                LoaiNhac? loai = null;
                var guiBm = false;
                if (!daNop && TienDoRules.NenNhacTruoc(homNay, han)) loai = LoaiNhac.MocTienDo;
                else if (!daNop && TienDoRules.QuaHan(homNay, han)) { loai = LoaiNhac.QuaHanMocTienDo; guiBm = true; }
                else if (daNop && ketQua == KetQuaTienDo.CanhBao) { loai = LoaiNhac.CanhBaoBaoCaoTienDo; guiBm = true; }
                if (loai is null)
                {
                    soBoQua++;
                    continue;
                }
                var (nguoiNhan, gui) = await GuiLanDauAsync(loai.Value, hs, han, gv, guiBm, ct);
                soThongBao += nguoiNhan;
                soEmail += gui;
                if (nguoiNhan == 0) soBoQua++;
            }
        }
        logger.LogInformation("Nhắc việc {Ngay}: T04 {T04}, email {Email}, thông báo {ThongBao}, bỏ qua {BoQua}.",
            homNay, chuyenT04, soEmail, soThongBao, soBoQua);
        return new KetQuaNhacViec(chuyenT04, soEmail, soThongBao, soBoQua);
    }

    /// <summary>T04 (DA_GIAO → DANG_THUC_HIEN) khi đợt đã đến ngày bắt đầu. Lỗi một hồ sơ không dừng job.</summary>
    private async Task<int> ChuyenT04Async(DateOnly homNay, CancellationToken ct)
    {
        var ids = await db.SVTrongDot.AsNoTracking()
            .Where(x => x.TrangThai == TrangThaiSVTrongDot.DA_GIAO && x.Dot.TrangThai != TrangThaiDot.Dong && x.Dot.NgayBatDau <= homNay)
            .Select(x => x.Id).ToListAsync(ct);
        var n = 0;
        foreach (var id in ids)
        {
            var hs = await db.SVTrongDot.SingleAsync(x => x.Id == id, ct);
            try
            {
                await workflow.TransitionHeThongAsync(hs.Id, new WorkflowTransitionRequest(Convert.ToBase64String(hs.RowVersion),
                    MaChuyen: "T04", LyDo: "Đến ngày bắt đầu đợt."), ct);
                n++;
            }
            catch (DomainException ex)
            {
                logger.LogWarning("Không chuyển T04 cho hồ sơ {Id}: {Code}", id, ex.Code);
            }
            db.ChangeTracker.Clear();
        }
        return n;
    }

    /// <summary>
    /// Ghi nhận nhắc (UK) rồi gửi cho người nhận. Trả về (số thông báo tạo, số email gửi); (0, 0) nếu đã gửi trước đó.
    /// </summary>
    private async Task<(int ThongBao, int Email)> GuiLanDauAsync(LoaiNhac loai, HoSoNhac hs, DateOnly han, GvNhac? gv, bool guiBm, CancellationToken ct)
    {
        if (await db.LichNhacViec.AnyAsync(x => x.SVTrongDotId == hs.Id && x.LoaiNhac == loai && x.HanNgay == han, ct))
            return (0, 0);
        db.LichNhacViec.Add(new LichNhacViec
        {
            SVTrongDotId = hs.Id,
            LoaiNhac = loai,
            HanNgay = han,
            DaGui = true,
            GuiLuc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        var (tieuDe, noiDung) = NoiDung(loai, hs, han);
        var nguoi = await NguoiNhanAsync(hs, gv, guiBm, ct);
        var thongBao = 0;
        var emailGui = 0;
        foreach (var n in nguoi)
        {
            if (n.TaiKhoanId is { } tk)
            {
                db.ThongBao.Add(new ThongBao { TaiKhoanId = tk, TieuDe = tieuDe, NoiDung = noiDung });
                thongBao++;
            }
            if (n.Email is { Length: > 0 } addr)
            {
                try
                {
                    await email.GuiAsync(addr, tieuDe, noiDung, ct);
                    emailGui++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Lỗi gửi email nhắc việc tới {Email}.", addr);
                }
            }
        }
        await db.SaveChangesAsync(ct);
        return (thongBao, emailGui);
    }

    private async Task<List<(long? TaiKhoanId, string? Email)>> NguoiNhanAsync(HoSoNhac hs, GvNhac? gv, bool guiBm, CancellationToken ct)
    {
        var ds = new List<(long? TaiKhoanId, string? Email)>();
        var tkSv = await db.TaiKhoan.AsNoTracking().Where(t => t.SinhVienId == hs.SinhVienId).Select(t => new { t.Id, t.Email }).FirstOrDefaultAsync(ct);
        ds.Add((tkSv?.Id, hs.EmailSv ?? tkSv?.Email)); // ưu tiên email nghiệp vụ của SV
        if (gv is not null)
        {
            var tkGv = await db.TaiKhoan.AsNoTracking().Where(t => t.GiangVienId == gv.GiangVienId).Select(t => new { t.Id, t.Email }).FirstOrDefaultAsync(ct);
            ds.Add((tkGv?.Id, gv.Email ?? tkGv?.Email));
            if (guiBm)
            {
                var bm = await db.TaiKhoanVaiTro.AsNoTracking()
                    .Where(x => x.BoMonId == gv.BoMonId && x.VaiTro.MaVaiTro == VaiTroMa.TruongBoMon)
                    .Select(x => new { x.TaiKhoan.Id, x.TaiKhoan.Email }).Distinct().ToListAsync(ct);
                ds.AddRange(bm.Select(x => ((long?)x.Id, x.Email)));
            }
        }
        return ds;
    }

    private static (string TieuDe, string NoiDung) NoiDung(LoaiNhac loai, HoSoNhac hs, DateOnly han)
    {
        var tenHan = han.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
        return loai switch
        {
            LoaiNhac.MocTienDo => ($"Sắp đến hạn nộp báo cáo tiến độ – {hs.MaSV}",
                $"Sinh viên {hs.HoTen} ({hs.MaSV}) cần nộp báo cáo tiến độ trước {tenHan}."),
            LoaiNhac.QuaHanMocTienDo => ($"[C01] Chưa nộp báo cáo tiến độ quá hạn – {hs.MaSV}",
                $"Sinh viên {hs.HoTen} ({hs.MaSV}) chưa nộp báo cáo tiến độ, đã quá hạn {tenHan}."),
            _ => ($"[C02] Báo cáo tiến độ dưới yêu cầu – {hs.MaSV}",
                $"Báo cáo tiến độ của {hs.HoTen} ({hs.MaSV}) có tỉ lệ hoàn thành dưới yêu cầu (hạn {tenHan})."),
        };
    }

    private sealed record HoSoNhac(long Id, long DotId, long SinhVienId, string MaSV, string HoTen, string? EmailSv);

    private sealed record GvNhac(long GiangVienId, long BoMonId, string HoTen, string? Email);
}
