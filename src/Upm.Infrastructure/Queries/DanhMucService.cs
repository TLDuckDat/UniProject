using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Upm.Application.Common;
using Upm.Application.DanhMuc;
using Upm.Application.PhanQuyen;
using Upm.Domain.Common;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Infrastructure.Persistence;

namespace Upm.Infrastructure.Queries;

public sealed class DanhMucService(UpmDbContext db, IScopeFilter scope) : IDanhMucService
{
    public async Task<PagedResult<DanhMucItem>> DanhSachAsync(string loai, DanhMucQuery query, CancellationToken ct = default)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var key = ChuanHoa(loai);
        return key switch
        {
            "coso" => await DanhSach(await scope.LocAsync(db.CoSo.AsNoTracking(), ct: ct), query, page, "MaCoSo", "TenCoSo", x => new(x.Id, x.MaCoSo, x.TenCoSo, ConHoatDong: x.ConHoatDong), ct),
            "khoa" => await DanhSach(await scope.LocAsync(db.Khoa.AsNoTracking().Include(x => x.CoSo), ct: ct), query, page, "MaKhoa", "TenKhoa", x => new(x.Id, x.MaKhoa, x.TenKhoa, x.CoSoId, x.CoSo.TenCoSo, x.CoSoId), ct),
            "bo-mon" => await DanhSach(await scope.LocAsync(db.BoMon.AsNoTracking().Include(x => x.Khoa), ct: ct), query, page, "MaBM", "TenBM", x => new(x.Id, x.MaBM, x.TenBM, x.KhoaId, x.Khoa.TenKhoa), ct),
            "giang-vien" => await DanhSach(await scope.LocAsync(db.GiangVien.AsNoTracking().Include(x => x.BoMon), ct: ct), query, page, "MaGV", "HoTen", x => new(x.Id, x.MaGV, x.HoTen, x.BoMonId, x.BoMon.TenBM, x.CoSoId, x.Email, x.LoaiGV.ToString(), x.QuotaToiDa, ConHoatDong: x.ConHoatDong), ct),
            "phong-bao-ve" => await DanhSach(await scope.LocAsync(db.PhongBaoVe.AsNoTracking().Include(x => x.CoSo), ct: ct), query, page, "TenPhong", "TenPhong", x => new(x.Id, x.TenPhong, x.TenPhong, x.CoSoId, x.CoSo.TenCoSo, x.CoSoId, SoLuong: x.SucChua, ConHoatDong: x.ConSuDung), ct),
            "chuong-trinh-dao-tao" => await DanhSach(await scope.LocAsync(db.ChuongTrinhDaoTao.AsNoTracking().Include(x => x.Khoa), ct: ct), query, page, "MaCTDT", "TenCTDT", x => new(x.Id, x.MaCTDT, x.TenCTDT, x.KhoaId, x.Khoa.TenKhoa, Nganh: x.Nganh, He: x.He.ToString(), ChuongTrinh: x.ChuongTrinh.ToString(), TongTCYeuCau: x.TongTCYeuCau, TCDATN: x.TCDATN, TCTTTN: x.TCTTTN, CoHocPhanThayThe: x.CoHocPhanThayThe), ct),
            "hoc-phan" => await DanhSachHocPhan(query, page, ct),
            "ctdt-hoc-phan" => await DanhSachCtdtHocPhan(query, page, ct),
            _ => throw LoiLoai(loai),
        };
    }

    public async Task<DanhMucItem> LayAsync(string loai, long id, CancellationToken ct = default)
    {
        return ChuanHoa(loai) switch
        {
            "coso" => Map(await Tim(await scope.LocAsync(db.CoSo.AsNoTracking(), ct: ct), id, ct)),
            "khoa" => Map(await Tim(await scope.LocAsync(db.Khoa.AsNoTracking().Include(x => x.CoSo), ct: ct), id, ct)),
            "bo-mon" => Map(await Tim(await scope.LocAsync(db.BoMon.AsNoTracking().Include(x => x.Khoa), ct: ct), id, ct)),
            "giang-vien" => Map(await Tim(await scope.LocAsync(db.GiangVien.AsNoTracking().Include(x => x.BoMon), ct: ct), id, ct)),
            "phong-bao-ve" => Map(await Tim(await scope.LocAsync(db.PhongBaoVe.AsNoTracking().Include(x => x.CoSo), ct: ct), id, ct)),
            "chuong-trinh-dao-tao" => Map(await Tim(await scope.LocAsync(db.ChuongTrinhDaoTao.AsNoTracking().Include(x => x.Khoa), ct: ct), id, ct)),
            "hoc-phan" => Map(await Tim(await HocPhanCoPhamVi(ct), id, ct)),
            "ctdt-hoc-phan" => Map(await Tim(await scope.LocAsync(db.CTDTHocPhan.AsNoTracking().Include(x => x.HocPhan).Include(x => x.CTDT), ct: ct), id, ct)),
            _ => throw LoiLoai(loai),
        };
    }

    public async Task<DanhMucItem> TaoAsync(string loai, DanhMucRequest request, CancellationToken ct = default)
    {
        var r = request;
        KiemTraRequest(ChuanHoa(loai), r);
        var entity = await TaoEntityAsync(ChuanHoa(loai), r, ct);
        await db.SaveChangesAsync(ct);
        return await LayAsync(loai, entity, ct);
    }

    public async Task<DanhMucItem> SuaAsync(string loai, long id, DanhMucRequest request, CancellationToken ct = default)
    {
        var r = request;
        var key = ChuanHoa(loai);
        KiemTraRequest(key, r);
        switch (key)
        {
            case "coso": { var x = await Tim(await scope.LocAsync(db.CoSo, ct: ct), id, ct); x.MaCoSo = r.Ma; x.TenCoSo = r.Ten; x.ConHoatDong = r.ConHoatDong ?? x.ConHoatDong; break; }
            case "khoa": { var x = await Tim(await scope.LocAsync(db.Khoa, ct: ct), id, ct); x.MaKhoa = r.Ma; x.TenKhoa = r.Ten; x.CoSoId = BatBuoc(r.ParentId, "CoSoId"); x.KhoiNganh = EnumVal<KhoiNganh>(r.Loai); break; }
            case "bo-mon": { var x = await Tim(await scope.LocAsync(db.BoMon, ct: ct), id, ct); x.MaBM = r.Ma; x.TenBM = r.Ten; x.KhoaId = BatBuoc(r.ParentId, "KhoaId"); break; }
            case "giang-vien": { var x = await Tim(await scope.LocAsync(db.GiangVien, ct: ct), id, ct); x.MaGV = r.Ma; x.HoTen = r.Ten; x.BoMonId = BatBuoc(r.ParentId, "BoMonId"); x.CoSoId = BatBuoc(r.CoSoId, "CoSoId"); x.Email = r.Email; x.LoaiGV = EnumVal<LoaiGiangVien>(r.Loai); x.QuotaToiDa = r.SoLuong ?? x.QuotaToiDa; x.ConHoatDong = r.ConHoatDong ?? x.ConHoatDong; break; }
            case "phong-bao-ve": { var x = await Tim(await scope.LocAsync(db.PhongBaoVe, ct: ct), id, ct); x.TenPhong = r.Ten; x.CoSoId = BatBuoc(r.CoSoId ?? r.ParentId, "CoSoId"); x.SucChua = r.SoLuong ?? x.SucChua; x.ConSuDung = r.ConHoatDong ?? x.ConSuDung; break; }
            case "chuong-trinh-dao-tao": { var x = await Tim(await scope.LocAsync(db.ChuongTrinhDaoTao, ct: ct), id, ct); Gan(x, r); break; }
            case "hoc-phan": { var x = await Tim(await HocPhanCoPhamVi(ct), id, ct); x.MaHP = r.Ma; x.TenHP = r.Ten; x.SoTC = r.SoTinChi ?? x.SoTC; x.LoaiHP = EnumVal<LoaiHocPhan>(r.Loai); x.TinhDiem = r.TinhDiem ?? x.TinhDiem; break; }
            case "ctdt-hoc-phan": { var x = await Tim(await scope.LocAsync(db.CTDTHocPhan, ct: ct), id, ct); x.CTDTId = BatBuoc(r.ParentId, "CTDTId"); x.HocPhanId = BatBuoc(r.HocPhanId, "HocPhanId"); x.HocKyKeHoach = r.HocKyKeHoach ?? x.HocKyKeHoach; x.LaHPThayTheDATN = r.LaHPThayTheDATN ?? x.LaHPThayTheDATN; break; }
            default: throw LoiLoai(loai);
        }
        await db.SaveChangesAsync(ct);
        return await LayAsync(loai, id, ct);
    }

    public async Task XoaAsync(string loai, long id, CancellationToken ct = default)
    {
        switch (ChuanHoa(loai))
        {
            case "coso": (await Tim(await scope.LocAsync(db.CoSo, ct: ct), id, ct)).ConHoatDong = false; break;
            case "khoa": db.Khoa.Remove(await Tim(await scope.LocAsync(db.Khoa, ct: ct), id, ct)); break;
            case "bo-mon": db.BoMon.Remove(await Tim(await scope.LocAsync(db.BoMon, ct: ct), id, ct)); break;
            case "giang-vien": (await Tim(await scope.LocAsync(db.GiangVien, ct: ct), id, ct)).ConHoatDong = false; break;
            case "phong-bao-ve": (await Tim(await scope.LocAsync(db.PhongBaoVe, ct: ct), id, ct)).ConSuDung = false; break;
            case "chuong-trinh-dao-tao": db.ChuongTrinhDaoTao.Remove(await Tim(await scope.LocAsync(db.ChuongTrinhDaoTao, ct: ct), id, ct)); break;
            case "hoc-phan": (await Tim(await HocPhanCoPhamVi(ct), id, ct)).TinhDiem = false; break;
            case "ctdt-hoc-phan": db.CTDTHocPhan.Remove(await Tim(await scope.LocAsync(db.CTDTHocPhan, ct: ct), id, ct)); break;
            default: throw LoiLoai(loai);
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<long> TaoEntityAsync(string key, DanhMucRequest r, CancellationToken ct)
    {
        switch (key)
        {
            case "coso": { var x = new CoSo { MaCoSo = r.Ma, TenCoSo = r.Ten, ConHoatDong = r.ConHoatDong ?? true }; db.CoSo.Add(x); await db.SaveChangesAsync(ct); return x.Id; }
            case "khoa": { var x = new Khoa { MaKhoa = r.Ma, TenKhoa = r.Ten, CoSoId = BatBuoc(r.ParentId, "CoSoId"), KhoiNganh = EnumVal<KhoiNganh>(r.Loai) }; db.Khoa.Add(x); await db.SaveChangesAsync(ct); return x.Id; }
            case "bo-mon": { var x = new BoMon { MaBM = r.Ma, TenBM = r.Ten, KhoaId = BatBuoc(r.ParentId, "KhoaId") }; db.BoMon.Add(x); await db.SaveChangesAsync(ct); return x.Id; }
            case "giang-vien": { var x = new GiangVien { MaGV = r.Ma, HoTen = r.Ten, BoMonId = BatBuoc(r.ParentId, "BoMonId"), CoSoId = BatBuoc(r.CoSoId, "CoSoId"), Email = r.Email, LoaiGV = EnumVal<LoaiGiangVien>(r.Loai), QuotaToiDa = r.SoLuong ?? 5, ConHoatDong = r.ConHoatDong ?? true }; db.GiangVien.Add(x); await db.SaveChangesAsync(ct); return x.Id; }
            case "phong-bao-ve": { var x = new PhongBaoVe { TenPhong = r.Ten, CoSoId = BatBuoc(r.CoSoId ?? r.ParentId, "CoSoId"), SucChua = r.SoLuong ?? 1, ConSuDung = r.ConHoatDong ?? true }; db.PhongBaoVe.Add(x); await db.SaveChangesAsync(ct); return x.Id; }
            case "chuong-trinh-dao-tao": { var x = new ChuongTrinhDaoTao(); Gan(x, r); db.ChuongTrinhDaoTao.Add(x); await db.SaveChangesAsync(ct); return x.Id; }
            case "hoc-phan": { var x = new HocPhan { MaHP = r.Ma, TenHP = r.Ten, SoTC = r.SoTinChi ?? 0, LoaiHP = EnumVal<LoaiHocPhan>(r.Loai), TinhDiem = r.TinhDiem ?? true }; db.HocPhan.Add(x); await db.SaveChangesAsync(ct); return x.Id; }
            case "ctdt-hoc-phan": { var x = new CTDTHocPhan { CTDTId = BatBuoc(r.ParentId, "CTDTId"), HocPhanId = BatBuoc(r.HocPhanId, "HocPhanId"), HocKyKeHoach = r.HocKyKeHoach ?? 1, LaHPThayTheDATN = r.LaHPThayTheDATN ?? false }; db.CTDTHocPhan.Add(x); await db.SaveChangesAsync(ct); return x.Id; }
            default: throw LoiLoai(key);
        }
    }

    private async Task<PagedResult<DanhMucItem>> DanhSachHocPhan(DanhMucQuery q, PageRequest p, CancellationToken ct)
    {
        var links = await scope.LocAsync(db.CTDTHocPhan.AsNoTracking(), ct: ct);
        var ids = await links.Select(x => x.HocPhanId).Distinct().ToListAsync(ct);
        return await DanhSach(db.HocPhan.AsNoTracking().Where(x => ids.Contains(x.Id)), q, p, "MaHP", "TenHP",
            x => new(x.Id, x.MaHP, x.TenHP, SoTinChi: x.SoTC, Loai: x.LoaiHP.ToString(), TinhDiem: x.TinhDiem), ct);
    }

    private async Task<PagedResult<DanhMucItem>> DanhSachCtdtHocPhan(DanhMucQuery q, PageRequest p, CancellationToken ct)
    {
        var query = await scope.LocAsync(db.CTDTHocPhan.AsNoTracking().Include(x => x.HocPhan).Include(x => x.CTDT), ct: ct);
        if (!string.IsNullOrWhiteSpace(q.Q)) query = query.Where(x => x.HocPhan.MaHP.Contains(q.Q) || x.HocPhan.TenHP.Contains(q.Q));
        var total = await query.CountAsync(ct);
        var ordered = q.Desc ? query.OrderByDescending(x => x.HocPhan.MaHP) : query.OrderBy(x => x.HocPhan.MaHP);
        var rows = await ordered.Skip(p.Skip).Take(p.SafePageSize).ToListAsync(ct);
        return new PagedResult<DanhMucItem>(rows.Select(x => new DanhMucItem(x.Id, x.HocPhan.MaHP, x.HocPhan.TenHP, x.CTDTId,
            x.CTDT.TenCTDT, SoTinChi: x.HocPhan.SoTC, Loai: x.HocPhan.LoaiHP.ToString(), HocKyKeHoach: x.HocKyKeHoach,
            LaHPThayTheDATN: x.LaHPThayTheDATN, HocPhanId: x.HocPhanId)).ToArray(), p.SafePage, p.SafePageSize, total);
    }

    private async Task<IQueryable<HocPhan>> HocPhanCoPhamVi(CancellationToken ct)
    {
        var links = await scope.LocAsync(db.CTDTHocPhan.AsQueryable(), ct: ct);
        var ids = await links.Select(x => x.HocPhanId).Distinct().ToListAsync(ct);
        return db.HocPhan.Where(x => ids.Contains(x.Id));
    }

    private static async Task<PagedResult<DanhMucItem>> DanhSach<T>(IQueryable<T> query, DanhMucQuery request, PageRequest page,
        string code, string name, Func<T, DanhMucItem> map, CancellationToken ct) where T : class
    {
        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var term = request.Q.Trim();
            query = query.Where(x => EF.Property<string>(x, code).Contains(term) || EF.Property<string>(x, name).Contains(term));
        }
        var total = await query.CountAsync(ct);
        var sort = request.Sort is "Ten" or "ten" or "name" ? name : code;
        query = SapXep(query, sort, request.Desc);
        var rows = await query.Skip(page.Skip).Take(page.SafePageSize).ToListAsync(ct);
        return new PagedResult<DanhMucItem>(rows.Select(map).ToList(), page.SafePage, page.SafePageSize, total);
    }

    private static IQueryable<T> SapXep<T>(IQueryable<T> query, string property, bool desc)
    {
        var param = Expression.Parameter(typeof(T), "x");
        var body = Expression.Call(typeof(EF), nameof(EF.Property), [typeof(string)], param, Expression.Constant(property));
        var lambda = Expression.Lambda<Func<T, string>>(body, param);
        return desc ? query.OrderByDescending(lambda) : query.OrderBy(lambda);
    }

    private static void Gan(ChuongTrinhDaoTao x, DanhMucRequest r)
    {
        x.MaCTDT = r.Ma; x.TenCTDT = r.Ten; x.KhoaId = BatBuoc(r.ParentId, "KhoaId"); x.Nganh = r.Nganh ?? "";
        x.He = EnumVal<HeDaoTao>(r.He); x.ChuongTrinh = EnumVal<LoaiChuongTrinh>(r.ChuongTrinh);
        x.TongTCYeuCau = r.TongTCYeuCau ?? 0; x.TCDATN = r.TCDATN ?? 0; x.TCTTTN = r.TCTTTN ?? 0;
        x.CoHocPhanThayThe = r.CoHocPhanThayThe ?? false;
    }

    private static DanhMucItem Map(CoSo x) => new(x.Id, x.MaCoSo, x.TenCoSo, ConHoatDong: x.ConHoatDong);
    private static DanhMucItem Map(Khoa x) => new(x.Id, x.MaKhoa, x.TenKhoa, x.CoSoId, x.CoSo.TenCoSo, x.CoSoId, Loai: x.KhoiNganh.ToString());
    private static DanhMucItem Map(BoMon x) => new(x.Id, x.MaBM, x.TenBM, x.KhoaId, x.Khoa.TenKhoa, x.Khoa.CoSoId);
    private static DanhMucItem Map(GiangVien x) => new(x.Id, x.MaGV, x.HoTen, x.BoMonId, x.BoMon.TenBM, x.CoSoId, x.Email, x.LoaiGV.ToString(), x.QuotaToiDa, ConHoatDong: x.ConHoatDong);
    private static DanhMucItem Map(PhongBaoVe x) => new(x.Id, x.TenPhong, x.TenPhong, x.CoSoId, x.CoSo.TenCoSo, x.CoSoId, SoLuong: x.SucChua, ConHoatDong: x.ConSuDung);
    private static DanhMucItem Map(ChuongTrinhDaoTao x) => new(x.Id, x.MaCTDT, x.TenCTDT, x.KhoaId, x.Khoa.TenKhoa, Nganh: x.Nganh, He: x.He.ToString(), ChuongTrinh: x.ChuongTrinh.ToString(), TongTCYeuCau: x.TongTCYeuCau, TCDATN: x.TCDATN, TCTTTN: x.TCTTTN, CoHocPhanThayThe: x.CoHocPhanThayThe);
    private static DanhMucItem Map(HocPhan x) => new(x.Id, x.MaHP, x.TenHP, SoTinChi: x.SoTC, Loai: x.LoaiHP.ToString(), TinhDiem: x.TinhDiem);
    private static DanhMucItem Map(CTDTHocPhan x) => new(x.Id, x.HocPhan.MaHP, x.HocPhan.TenHP, x.CTDTId, x.CTDT.TenCTDT, SoTinChi: x.HocPhan.SoTC, Loai: x.HocPhan.LoaiHP.ToString(), HocKyKeHoach: x.HocKyKeHoach, LaHPThayTheDATN: x.LaHPThayTheDATN, HocPhanId: x.HocPhanId);

    private static TEnum EnumVal<TEnum>(string? value) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : throw new DomainException("UPM_DANH_MUC_ENUM_KHONG_HOP_LE", $"Giá trị danh mục không hợp lệ: {value ?? "(trống)"}.");

    private static long BatBuoc(long? id, string field) => id is > 0 ? id.Value : throw new DomainException("UPM_DANH_MUC_THIEU_KHOA_NGOAI", $"Thiếu {field} hợp lệ.");
    private static void KiemTraRequest(string loai, DanhMucRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Ten) || (loai is not "phong-bao-ve" && string.IsNullOrWhiteSpace(request.Ma)))
            throw new DomainException("UPM_DANH_MUC_THIEU_THONG_TIN", "Mã và tên danh mục là bắt buộc.");
        if (loai == "giang-vien" && request.SoLuong is < 5 or > 10)
            throw new DomainException("UPM_DANH_MUC_QUOTA_GV_KHONG_HOP_LE", "Quota giảng viên phải từ 5 đến 10 sinh viên mỗi đợt.");
        if (loai == "phong-bao-ve" && request.SoLuong is <= 0)
            throw new DomainException("UPM_DANH_MUC_SUC_CHUA_KHONG_HOP_LE", "Sức chứa phòng phải lớn hơn 0.");
        if (loai == "hoc-phan" && request.SoTinChi is < 0)
            throw new DomainException("UPM_DANH_MUC_SO_TC_KHONG_HOP_LE", "Số tín chỉ không được âm.");
    }
    private static string ChuanHoa(string loai) => loai.Trim().ToLowerInvariant();
    private static DomainException LoiLoai(string loai) => new("UPM_DANH_MUC_LOAI_KHONG_HOP_LE", $"Không hỗ trợ danh mục '{loai}'.");
    private static DomainException NotFound(string loai) => new("UPM_DANH_MUC_NOT_FOUND", $"Không tìm thấy bản ghi thuộc danh mục '{loai}' hoặc ngoài phạm vi của bạn.");
    private static async Task<T> Tim<T>(IQueryable<T> query, long id, CancellationToken ct) where T : class =>
        await query.SingleOrDefaultAsync(x => EF.Property<long>(x, "Id") == id, ct) ?? throw NotFound(typeof(T).Name);
}

public sealed class TaiKhoanQuanLyService(UpmDbContext db, IPasswordHasher<TaiKhoan> hasher) : ITaiKhoanQuanLyService
{
    public async Task<PagedResult<TaiKhoanItem>> DanhSachAsync(string? q, PageRequest page, CancellationToken ct = default)
    {
        var query = db.TaiKhoan.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.TenDangNhap.Contains(q) || (x.Email != null && x.Email.Contains(q)));
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.TenDangNhap).Skip(page.Skip).Take(page.SafePageSize).ToListAsync(ct);
        var ids = rows.Select(x => x.Id).ToArray();
        var roles = await db.TaiKhoanVaiTro.AsNoTracking().Include(x => x.VaiTro).Where(x => ids.Contains(x.TaiKhoanId)).ToListAsync(ct);
        return new PagedResult<TaiKhoanItem>(rows.Select(x => new TaiKhoanItem(x.Id, x.TenDangNhap, x.Email, x.LoaiTK.ToString(), x.ConHoatDong,
            x.GiangVienId, x.SinhVienId, roles.Where(v => v.TaiKhoanId == x.Id).Select(v => new TaiKhoanVaiTroRequest(v.VaiTro.MaVaiTro, v.CoSoId, v.KhoaId, v.BoMonId)).ToArray())).ToArray(), page.SafePage, page.SafePageSize, total);
    }

    public async Task<TaiKhoanItem> TaoAsync(TaiKhoanRequest request, CancellationToken ct = default)
    {
        var x = new TaiKhoan { TenDangNhap = request.TenDangNhap.Trim(), Email = request.Email, LoaiTK = ParseLoai(request.LoaiTK), ConHoatDong = request.ConHoatDong, GiangVienId = request.GiangVienId, SinhVienId = request.SinhVienId };
        if (!string.IsNullOrWhiteSpace(request.MatKhau)) x.MatKhauHash = hasher.HashPassword(x, request.MatKhau);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        db.TaiKhoan.Add(x);
        await db.SaveChangesAsync(ct);
        await GanVaiTro(x.Id, request.VaiTro, ct);
        await tx.CommitAsync(ct);
        return (await DanhSachAsync(x.TenDangNhap, new PageRequest(1, 1), ct)).Items.Single();
    }

    public async Task<TaiKhoanItem> SuaAsync(long id, TaiKhoanRequest request, CancellationToken ct = default)
    {
        var x = await db.TaiKhoan.SingleOrDefaultAsync(a => a.Id == id, ct) ?? throw new DomainException("UPM_TAI_KHOAN_NOT_FOUND", "Không tìm thấy tài khoản.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        x.TenDangNhap = request.TenDangNhap.Trim(); x.Email = request.Email; x.LoaiTK = ParseLoai(request.LoaiTK); x.ConHoatDong = request.ConHoatDong; x.GiangVienId = request.GiangVienId; x.SinhVienId = request.SinhVienId;
        if (!string.IsNullOrWhiteSpace(request.MatKhau)) x.MatKhauHash = hasher.HashPassword(x, request.MatKhau);
        await db.SaveChangesAsync(ct);
        await GanVaiTro(id, request.VaiTro, ct);
        await tx.CommitAsync(ct);
        return (await DanhSachAsync(x.TenDangNhap, new PageRequest(1, 1), ct)).Items.Single();
    }

    public async Task XoaAsync(long id, CancellationToken ct = default)
    {
        var account = await db.TaiKhoan.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new DomainException("UPM_TAI_KHOAN_NOT_FOUND", "Không tìm thấy tài khoản.");
        account.ConHoatDong = false;
        await db.SaveChangesAsync(ct);
    }

    private async Task GanVaiTro(long taiKhoanId, IReadOnlyList<TaiKhoanVaiTroRequest> roles, CancellationToken ct)
    {
        db.TaiKhoanVaiTro.RemoveRange(db.TaiKhoanVaiTro.Where(x => x.TaiKhoanId == taiKhoanId));
        foreach (var r in roles)
        {
            var count = (r.CoSoId is not null ? 1 : 0) + (r.KhoaId is not null ? 1 : 0) + (r.BoMonId is not null ? 1 : 0);
            if (count > 1) throw new DomainException("UPM_TAI_KHOAN_PHAM_VI_KHONG_HOP_LE", "Mỗi vai trò chỉ được gán một phạm vi cụ thể; null là toàn hệ thống.");
            var role = await db.VaiTro.SingleOrDefaultAsync(x => x.MaVaiTro == r.MaVaiTro, ct) ?? throw new DomainException("UPM_TAI_KHOAN_VAI_TRO_NOT_FOUND", $"Không tìm thấy vai trò {r.MaVaiTro}.");
            if (r.CoSoId is { } coSo && !await db.CoSo.AnyAsync(x => x.Id == coSo, ct)) throw new DomainException("UPM_TAI_KHOAN_PHAM_VI_KHONG_HOP_LE", "Cơ sở không tồn tại.");
            if (r.KhoaId is { } khoa && !await db.Khoa.AnyAsync(x => x.Id == khoa, ct)) throw new DomainException("UPM_TAI_KHOAN_PHAM_VI_KHONG_HOP_LE", "Khoa không tồn tại.");
            if (r.BoMonId is { } bm && !await db.BoMon.AnyAsync(x => x.Id == bm, ct)) throw new DomainException("UPM_TAI_KHOAN_PHAM_VI_KHONG_HOP_LE", "Bộ môn không tồn tại.");
            db.TaiKhoanVaiTro.Add(new TaiKhoanVaiTro { TaiKhoanId = taiKhoanId, VaiTroId = role.Id, CoSoId = r.CoSoId, KhoaId = r.KhoaId, BoMonId = r.BoMonId });
        }
        await db.SaveChangesAsync(ct);
    }

    private static LoaiTaiKhoan ParseLoai(string v) => Enum.TryParse<LoaiTaiKhoan>(v, true, out var parsed) ? parsed : throw new DomainException("UPM_TAI_KHOAN_LOAI_KHONG_HOP_LE", "Loại tài khoản không hợp lệ.");
}
