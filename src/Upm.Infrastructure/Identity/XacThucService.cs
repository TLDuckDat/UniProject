using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Upm.Application.Abstractions;
using Upm.Application.XacThuc;
using Upm.Domain.Common;
using Upm.Domain.Entities;
using Upm.Domain.PhanQuyen;
using Upm.Infrastructure.Persistence;

namespace Upm.Infrastructure.Identity;

public sealed class XacThucService(
    UpmDbContext db,
    TokenService tokens,
    IPasswordHasher<TaiKhoan> hasher,
    IOptions<JwtOptions> options,
    IClock clock,
    ICurrentUser currentUser,
    DangNhapSaiTracker dangNhapSai,
    ILogger<XacThucService> logger) : IXacThucService
{
    public const string SaiThongTin = "UPM_AUTH_SAI_THONG_TIN";
    public const string RefreshKhongHopLe = "UPM_AUTH_REFRESH_KHONG_HOP_LE";

    public const string TamKhoa = "UPM_AUTH_TAM_KHOA";

    public async Task<PhienDangNhap> DangNhapAsync(DangNhapRequest request, CancellationToken ct = default)
    {
        var ip = currentUser.IpAddress;

        // Cặp (tài khoản, IP) đang bị khóa tạm: từ chối trước khi kiểm mật khẩu (kể cả mật khẩu đúng).
        if (dangNhapSai.ConBiKhoa(request.TenDangNhap, ip) is { } conLai)
        {
            throw TamKhoaException(conLai);
        }

        var taiKhoan = await db.TaiKhoan.SingleOrDefaultAsync(t => t.TenDangNhap == request.TenDangNhap, ct);

        // Luôn chạy phép kiểm băm (kể cả khi không có tài khoản) để thời gian phản hồi không lộ tài khoản tồn tại.
        var ketQua = taiKhoan is { ConHoatDong: true, MatKhauHash: not null }
            ? KiemBam(taiKhoan, taiKhoan.MatKhauHash, request.MatKhau)
            : KiemBamGia(request.MatKhau);

        // Cùng một thông báo cho mọi trường hợp sai.
        if (taiKhoan is null || ketQua == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Đăng nhập thất bại cho {TenDangNhap} từ {Ip}", request.TenDangNhap, ip);
            if (dangNhapSai.GhiNhanSai(request.TenDangNhap, ip))
            {
                logger.LogWarning("Tạm khóa đăng nhập {TenDangNhap} từ {Ip} do sai quá nhiều lần", request.TenDangNhap, ip);
            }
            throw new XacThucException(SaiThongTin, "Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        dangNhapSai.XoaDem(request.TenDangNhap, ip);

        if (ketQua == PasswordVerificationResult.SuccessRehashNeeded)
        {
            taiKhoan.MatKhauHash = hasher.HashPassword(taiKhoan, request.MatKhau);
        }

        return await CapPhienAsync(taiKhoan, Guid.NewGuid(), ct);
    }

    public async Task<PhienDangNhap> LamMoiAsync(string refreshToken, CancellationToken ct = default)
    {
        var now = clock.Now.UtcDateTime;
        var hash = TokenService.BamRefreshToken(refreshToken);
        var cu = await db.RefreshToken.SingleOrDefaultAsync(r => r.TokenHash == hash, ct);

        if (cu is null)
        {
            throw KhongHopLe();
        }

        // Ân hạn: nhiều tab cùng refresh bằng token vừa xoay (trong 30 giây) – cấp phiên mới cùng họ, không coi là đánh cắp.
        var vuaXoay = cu is { LyDoThuHoi: "XoayVong", ThuHoiLuc: { } luc } && now - luc <= AnHanXoayVong;

        if (cu.ThuHoiLuc is not null && !vuaXoay)
        {
            // Token đã bị xoay/thu hồi mà vẫn được dùng lại → có thể bị đánh cắp: thu hồi cả họ.
            logger.LogWarning("Phát hiện dùng lại refresh token đã thu hồi (tài khoản {TaiKhoanId}) – thu hồi cả họ token.", cu.TaiKhoanId);
            await db.RefreshToken
                .Where(r => r.HoToken == cu.HoToken && r.ThuHoiLuc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.ThuHoiLuc, now).SetProperty(r => r.LyDoThuHoi, "DungLai"), ct);
            throw KhongHopLe();
        }

        if (cu.HetHanLuc <= now)
        {
            throw KhongHopLe();
        }

        var taiKhoan = await db.TaiKhoan.SingleAsync(t => t.Id == cu.TaiKhoanId, ct);
        if (!taiKhoan.ConHoatDong)
        {
            throw KhongHopLe();
        }

        return await CapPhienAsync(taiKhoan, cu.HoToken, ct, tokenCu: vuaXoay ? null : cu);
    }

    public async Task DangXuatAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return;
        }

        var hash = TokenService.BamRefreshToken(refreshToken);
        var token = await db.RefreshToken.SingleOrDefaultAsync(r => r.TokenHash == hash && r.ThuHoiLuc == null, ct);
        if (token is not null)
        {
            token.ThuHoiLuc = clock.Now.UtcDateTime;
            token.LyDoThuHoi = "DangXuat";
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<NguoiDungResponse> ThongTinAsync(long taiKhoanId, CancellationToken ct = default)
    {
        var tk = await db.TaiKhoan.AsNoTracking().SingleAsync(t => t.Id == taiKhoanId, ct);
        var hoTen = tk.GiangVienId is { } gv
            ? await db.GiangVien.Where(x => x.Id == gv).Select(x => x.HoTen).FirstOrDefaultAsync(ct)
            : tk.SinhVienId is { } sv
                ? await db.SinhVien.Where(x => x.Id == sv).Select(x => x.HoTen).FirstOrDefaultAsync(ct)
                : null;

        var phamVi = await QuyenHienTai.NapPhamViAsync(db, taiKhoanId, ct);
        var tenVaiTro = await db.VaiTro.ToDictionaryAsync(v => v.MaVaiTro, v => v.TenVaiTro, ct);
        var coSo = await db.CoSo.AsNoTracking().Where(c => c.ConHoatDong).ToListAsync(ct);
        var khoaIds = phamVi.Where(p => p.KhoaId is not null).Select(p => p.KhoaId!.Value).ToList();
        var boMonIds = phamVi.Where(p => p.BoMonId is not null).Select(p => p.BoMonId!.Value).ToList();
        var tenKhoa = await db.Khoa.Where(k => khoaIds.Contains(k.Id)).ToDictionaryAsync(k => k.Id, k => k.TenKhoa, ct);
        var tenBoMon = await db.BoMon.Where(b => boMonIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.TenBM, ct);

        var vaiTro = phamVi.Select(p => new PhamViResponse(
            p.MaVaiTro,
            tenVaiTro.GetValueOrDefault(p.MaVaiTro, p.MaVaiTro),
            p.CoSoId,
            coSo.FirstOrDefault(c => c.Id == p.CoSoId)?.MaCoSo,
            p.KhoaId,
            p.KhoaId is { } k ? tenKhoa.GetValueOrDefault(k) : null,
            p.BoMonId,
            p.BoMonId is { } b ? tenBoMon.GetValueOrDefault(b) : null)).ToList();

        var coSoDuocPhep = phamVi.Any(p => p.ToanHeThong)
            ? coSo
            : coSo.Where(c => phamVi.Any(p => p.CoSoId == c.Id)).ToList();

        return new NguoiDungResponse(
            tk.Id,
            tk.TenDangNhap,
            hoTen,
            tk.Email,
            tk.GiangVienId,
            tk.SinhVienId,
            vaiTro,
            BangQuyen.ChoVaiTro(phamVi.Select(p => p.MaVaiTro)).Order(StringComparer.Ordinal).ToList(),
            coSoDuocPhep.Select(c => new CoSoResponse(c.Id, c.MaCoSo, c.TenCoSo)).ToList());
    }

    private async Task<PhienDangNhap> CapPhienAsync(TaiKhoan taiKhoan, Guid hoToken, CancellationToken ct, RefreshToken? tokenCu = null)
    {
        var now = clock.Now;
        var refresh = TokenService.TaoRefreshToken();
        var refreshHetHan = now.AddDays(options.Value.RefreshTokenDays);

        var moi = new RefreshToken
        {
            TaiKhoanId = taiKhoan.Id,
            TokenHash = TokenService.BamRefreshToken(refresh),
            HoToken = hoToken,
            TaoLuc = now.UtcDateTime,
            HetHanLuc = refreshHetHan.UtcDateTime,
            TaoTuIp = currentUser.IpAddress,
        };
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            db.RefreshToken.Add(moi);
            await db.SaveChangesAsync(ct);

            if (tokenCu is not null)
            {
                tokenCu.ThuHoiLuc = now.UtcDateTime;
                tokenCu.LyDoThuHoi = "XoayVong";
                tokenCu.ThayTheBoiId = moi.Id;
                await db.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
        }

        var (access, hetHan) = tokens.TaoAccessToken(taiKhoan);
        var nguoiDung = await ThongTinAsync(taiKhoan.Id, ct);
        return new PhienDangNhap(new TokenResponse(access, hetHan, nguoiDung), refresh, refreshHetHan);
    }

    private static readonly TimeSpan AnHanXoayVong = TimeSpan.FromSeconds(30);

    // Hash cố định (của chuỗi ngẫu nhiên) để kiểm "giả" khi không có tài khoản – tốn cùng thời gian với kiểm thật.
    private static readonly Lazy<string> BamGia = new(() =>
        new PasswordHasher<TaiKhoan>().HashPassword(new TaiKhoan(), Guid.NewGuid().ToString()));

    /// <summary>Hash hỏng (sai định dạng) trong DB → coi như sai mật khẩu, không để lỗi 500.</summary>
    private PasswordVerificationResult KiemBam(TaiKhoan taiKhoan, string hash, string matKhau)
    {
        try
        {
            return hasher.VerifyHashedPassword(taiKhoan, hash, matKhau);
        }
        catch (FormatException)
        {
            logger.LogError("MatKhauHash của tài khoản {TaiKhoanId} sai định dạng – cần đặt lại mật khẩu.", taiKhoan.Id);
            return PasswordVerificationResult.Failed;
        }
    }

    private PasswordVerificationResult KiemBamGia(string matKhau)
    {
        hasher.VerifyHashedPassword(new TaiKhoan(), BamGia.Value, matKhau);
        return PasswordVerificationResult.Failed;
    }

    private static TamKhoaDangNhapException TamKhoaException(TimeSpan conLai) =>
        new(TamKhoa,
            $"Bạn đã nhập sai mật khẩu quá nhiều lần. Vui lòng thử lại sau {Math.Max(1, (int)Math.Ceiling(conLai.TotalMinutes))} phút.",
            conLai);

    private static XacThucException KhongHopLe() =>
        new(RefreshKhongHopLe, "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
}
