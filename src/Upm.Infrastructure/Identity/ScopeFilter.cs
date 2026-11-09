using System.Linq.Expressions;
using Upm.Application.PhanQuyen;
using Upm.Domain.Entities;

namespace Upm.Infrastructure.Identity;

/// <summary>
/// Cài đặt <see cref="IScopeFilter"/> theo CLAUDE.md §5.8: bảng GỐC có CoSoId (DotDATN, SVTrongDot, HoiDong, QuyetDinh,
/// GiangVien, SinhVien, Khoa, PhongBaoVe) lọc trực tiếp; bảng CON đi qua navigation tới bảng gốc
/// (vd PhieuDiem → LichBaoVe → HoiDong.CoSoId). EF dịch navigation thành JOIN.
/// Entity chưa khai báo → <see cref="NotSupportedException"/> (thà lỗi còn hơn lộ dữ liệu ngoài phạm vi).
/// </summary>
public sealed class ScopeFilter(IQuyenHienTai quyen) : IScopeFilter
{
    public async Task<IQueryable<T>> LocAsync<T>(IQueryable<T> query, IEnumerable<string>? chiVaiTro = null, CancellationToken ct = default)
        where T : class
    {
        var phamVi = (await quyen.LayAsync(ct)).PhamVi(chiVaiTro);
        if (!Bang.TryGetValue(typeof(T), out var selector))
        {
            throw new NotSupportedException($"Chưa khai báo phạm vi dữ liệu cho {typeof(T).Name} trong ScopeFilter.");
        }

        var s = (ScopeSelector<T>)selector;
        return query.Where(phamVi.TaoBieuThuc(s.CoSo, s.Khoa, s.BoMon));
    }

    /// <summary>Các loại entity đã khai báo phạm vi (dùng cho test).</summary>
    public static IReadOnlyCollection<Type> LoaiDaKhaiBao => Bang.Keys;

    internal sealed record ScopeSelector<T>(
        Expression<Func<T, long>> CoSo,
        Expression<Func<T, long?>>? Khoa,
        Expression<Func<T, long?>>? BoMon);

    private static ScopeSelector<T> S<T>(
        Expression<Func<T, long>> coSo,
        Expression<Func<T, long?>>? khoa = null,
        Expression<Func<T, long?>>? boMon = null) => new(coSo, khoa, boMon);

    private static readonly Dictionary<Type, object> Bang = new()
    {
        // ----- Danh mục tổ chức -----
        [typeof(CoSo)] = S<CoSo>(x => x.Id),
        [typeof(Khoa)] = S<Khoa>(x => x.CoSoId, x => x.Id),
        [typeof(BoMon)] = S<BoMon>(x => x.Khoa.CoSoId, x => x.KhoaId, x => x.Id),
        [typeof(ChuongTrinhDaoTao)] = S<ChuongTrinhDaoTao>(x => x.Khoa.CoSoId, x => x.KhoaId),
        [typeof(CTDTHocPhan)] = S<CTDTHocPhan>(x => x.CTDT.Khoa.CoSoId, x => x.CTDT.KhoaId),
        [typeof(ThamSoBoMon)] = S<ThamSoBoMon>(x => x.BoMon.Khoa.CoSoId, x => x.BoMon.KhoaId, x => x.BoMonId),
        [typeof(ThamSoDaoVan)] = S<ThamSoDaoVan>(x => x.Khoa.CoSoId, x => x.KhoaId),

        // ----- Bảng GỐC có CoSoId -----
        [typeof(GiangVien)] = S<GiangVien>(x => x.CoSoId, x => x.BoMon.KhoaId, x => x.BoMonId),
        // SV thuộc khoa quản lý CTĐT; SV không gắn bộ môn → phạm vi chỉ-bộ-môn không thấy SV qua bộ lọc này
        // (Trưởng BM thấy SV qua vai trò trên từng SV – PhanCong).
        [typeof(SinhVien)] = S<SinhVien>(x => x.CoSoId, x => x.CTDT.KhoaId),
        [typeof(SVTrongDot)] = S<SVTrongDot>(x => x.CoSoId, x => x.CTDT.KhoaId),
        [typeof(DotDATN)] = S<DotDATN>(x => x.CoSoId),
        [typeof(HoiDong)] = S<HoiDong>(x => x.CoSoId, x => x.KhoaId),
        [typeof(PhongBaoVe)] = S<PhongBaoVe>(x => x.CoSoId),
        [typeof(QuyetDinh)] = S<QuyetDinh>(x => x.CoSoId),

        // ----- Bảng CON: lọc qua bảng gốc -----
        [typeof(DeTai)] = S<DeTai>(x => x.Dot.CoSoId, x => x.BoMon.KhoaId, x => x.BoMonId),
        [typeof(MocTienDo)] = S<MocTienDo>(x => x.Dot.CoSoId),
        [typeof(PhanCong)] = S<PhanCong>(x => x.SVTrongDot.CoSoId, x => x.SVTrongDot.CTDT.KhoaId),
        [typeof(DonTu)] = S<DonTu>(x => x.SVTrongDot.CoSoId, x => x.SVTrongDot.CTDT.KhoaId),
        [typeof(PhienBanQuyen)] = S<PhienBanQuyen>(x => x.SVTrongDot.CoSoId, x => x.SVTrongDot.CTDT.KhoaId),
        [typeof(BaoCaoTienDo)] = S<BaoCaoTienDo>(x => x.SVTrongDot.CoSoId, x => x.SVTrongDot.CTDT.KhoaId),
        [typeof(DiemDATN)] = S<DiemDATN>(x => x.SVTrongDot.CoSoId, x => x.SVTrongDot.CTDT.KhoaId),
        [typeof(ThanhVienHoiDong)] = S<ThanhVienHoiDong>(x => x.HoiDong.CoSoId, x => x.HoiDong.KhoaId),
        [typeof(LichBaoVe)] = S<LichBaoVe>(x => x.HoiDong.CoSoId, x => x.HoiDong.KhoaId),
        [typeof(PhieuDiem)] = S<PhieuDiem>(x => x.LichBaoVe.HoiDong.CoSoId, x => x.LichBaoVe.HoiDong.KhoaId),
    };
}
