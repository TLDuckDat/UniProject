using System.Linq.Expressions;

namespace Upm.Domain.PhanQuyen;

/// <summary>
/// Một dòng TaiKhoanVaiTro đã chuẩn hóa. NULL = rộng hơn (CLAUDE.md §5.7):
/// cả 3 NULL → toàn hệ thống; chỉ CoSoId → cả cơ sở; KhoaId → cả khoa (gồm các bộ môn); BoMonId → riêng bộ môn.
/// <see cref="CoSoId"/> được suy ra từ Khoa/BoMon khi nạp (để biết tài khoản làm việc ở cơ sở nào).
/// </summary>
public sealed record PhamViVaiTro(string MaVaiTro, long? CoSoId, long? KhoaId, long? BoMonId)
{
    public bool ToanHeThong => CoSoId is null && KhoaId is null && BoMonId is null;
}

/// <summary>
/// Phạm vi dữ liệu hợp nhất từ các vai trò của người dùng, có thể thu hẹp theo cơ sở đang làm việc.
/// Logic thuần – dựng biểu thức lọc cho IQueryable (EF dịch sang SQL).
/// </summary>
public sealed class PhamViTruyCap
{
    private PhamViTruyCap(bool toanHeThong, IReadOnlySet<long> coSo, IReadOnlySet<long> khoa, IReadOnlySet<long> boMon, long? coSoDangLamViec)
    {
        ToanHeThong = toanHeThong;
        CoSoIds = coSo;
        KhoaIds = khoa;
        BoMonIds = boMon;
        CoSoDangLamViecId = coSoDangLamViec;
    }

    public bool ToanHeThong { get; }
    /// <summary>Cơ sở được xem TOÀN BỘ.</summary>
    public IReadOnlySet<long> CoSoIds { get; }
    public IReadOnlySet<long> KhoaIds { get; }
    public IReadOnlySet<long> BoMonIds { get; }
    /// <summary>Nếu có: chỉ lấy dữ liệu của cơ sở này (giao với phạm vi).</summary>
    public long? CoSoDangLamViecId { get; }

    public bool TrongRong => !ToanHeThong && CoSoIds.Count == 0 && KhoaIds.Count == 0 && BoMonIds.Count == 0;

    public static readonly PhamViTruyCap Rong = new(false, new HashSet<long>(), new HashSet<long>(), new HashSet<long>(), null);

    /// <summary>
    /// Hợp phạm vi của các vai trò. <paramref name="chiVaiTro"/> = null → mọi vai trò của người dùng.
    /// </summary>
    public static PhamViTruyCap Tu(IEnumerable<PhamViVaiTro> vaiTro, long? coSoDangLamViecId = null, IEnumerable<string>? chiVaiTro = null)
    {
        var loc = chiVaiTro?.ToHashSet(StringComparer.Ordinal);
        var dong = vaiTro.Where(v => loc is null || loc.Contains(v.MaVaiTro)).ToList();

        var toanHeThong = dong.Any(v => v.ToanHeThong);
        var coSo = dong.Where(v => v is { CoSoId: not null, KhoaId: null, BoMonId: null }).Select(v => v.CoSoId!.Value).ToHashSet();
        var khoa = dong.Where(v => v is { KhoaId: not null, BoMonId: null }).Select(v => v.KhoaId!.Value).ToHashSet();
        var boMon = dong.Where(v => v.BoMonId is not null).Select(v => v.BoMonId!.Value).ToHashSet();
        return new PhamViTruyCap(toanHeThong, coSo, khoa, boMon, coSoDangLamViecId);
    }

    /// <summary>Kiểm tra một bản ghi (cơ sở, khoa, bộ môn của nó) có nằm trong phạm vi.</summary>
    public bool ChoPhep(long coSoId, long? khoaId, long? boMonId)
    {
        if (CoSoDangLamViecId is { } cs && cs != coSoId)
        {
            return false;
        }

        return ToanHeThong
               || CoSoIds.Contains(coSoId)
               || (khoaId is { } k && KhoaIds.Contains(k))
               || (boMonId is { } b && BoMonIds.Contains(b));
    }

    /// <summary>
    /// Dựng predicate cho IQueryable từ các selector (cơ sở / khoa / bộ môn của entity).
    /// Selector khoa/bộ môn có thể là subquery EF; null nếu entity không gắn khoa/bộ môn.
    /// </summary>
    public Expression<Func<T, bool>> TaoBieuThuc<T>(
        Expression<Func<T, long>> coSo,
        Expression<Func<T, long?>>? khoa = null,
        Expression<Func<T, long?>>? boMon = null)
    {
        var p = Expression.Parameter(typeof(T), "x");
        Expression Body<TR>(Expression<Func<T, TR>> sel) => new ThayThamSo(sel.Parameters[0], p).Visit(sel.Body);

        Expression? phamVi = null;
        if (ToanHeThong)
        {
            phamVi = Expression.Constant(true);
        }
        else
        {
            if (CoSoIds.Count > 0)
            {
                phamVi = Or(phamVi, Contains(CoSoIds.ToList(), Body(coSo)));
            }
            if (khoa is not null && KhoaIds.Count > 0)
            {
                phamVi = Or(phamVi, Contains(KhoaIds.Select(x => (long?)x).ToList(), Body(khoa)));
            }
            if (boMon is not null && BoMonIds.Count > 0)
            {
                phamVi = Or(phamVi, Contains(BoMonIds.Select(x => (long?)x).ToList(), Body(boMon)));
            }
        }

        phamVi ??= Expression.Constant(false);
        if (CoSoDangLamViecId is { } cs)
        {
            phamVi = Expression.AndAlso(Expression.Equal(Body(coSo), Expression.Constant(cs)), phamVi);
        }

        return Expression.Lambda<Func<T, bool>>(phamVi, p);
    }

    private static Expression Or(Expression? left, Expression right) => left is null ? right : Expression.OrElse(left, right);

    private static MethodCallExpression Contains<TV>(List<TV> values, Expression value) =>
        Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), [typeof(TV)], Expression.Constant(values), value);

    private sealed class ThayThamSo(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
    }
}
