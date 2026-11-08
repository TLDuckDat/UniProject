using Upm.Domain.Common;

namespace Upm.Domain.Rules;

/// <summary>Một mức của thang điểm chữ: từ <see cref="TuDiem10"/> (đã làm tròn 1 chữ số) trở lên.</summary>
public sealed record MucDiemChu(decimal TuDiem10, string DiemChu, decimal Diem4);

/// <summary>Kết quả quy đổi một điểm thang 10.</summary>
public sealed record DiemQuyDoi(decimal Diem10, string DiemChu, decimal Diem4);

/// <summary>Quy đổi thang 10 → chữ → thang 4 (đặc tả mục 8, Sổ tay UTC2).</summary>
public static class QuyDoiDiem
{
    /// <summary>Thang điểm chữ theo Sổ tay SV UTC2 (đặc tả mục 8), xếp giảm dần theo ngưỡng.</summary>
    public static IReadOnlyList<MucDiemChu> ThangSoTayUTC2 { get; } =
    [
        new(9.5m, "A+", 4.0m),
        new(8.5m, "A", 3.8m),
        new(8.0m, "B+", 3.5m),
        new(7.0m, "B", 3.0m),
        new(6.0m, "C+", 2.5m),
        new(5.5m, "C", 2.0m),
        new(4.5m, "D+", 1.5m),
        new(4.0m, "D", 1.0m),
        new(2.0m, "F+", 0.5m),
        new(0.0m, "F", 0.0m),
    ];

    /// <summary>
    /// Làm tròn điểm về 1 chữ số (qua <see cref="LamTron.Diem"/>) rồi tra thang.
    /// <paramref name="thang"/> mặc định là <see cref="ThangSoTayUTC2"/>; truyền thang khác khi quy chế mới đổi.
    /// </summary>
    public static DiemQuyDoi TuThang10(decimal diem10, IReadOnlyList<MucDiemChu>? thang = null)
    {
        KiemTraDiem.Thang10(diem10, nameof(diem10));
        var tron = LamTron.Diem(diem10);
        var muc = (thang ?? ThangSoTayUTC2).OrderByDescending(m => m.TuDiem10).FirstOrDefault(m => tron >= m.TuDiem10)
            ?? throw new DomainException("UPM_THANG_DIEM_KHONG_HOP_LE", $"Thang điểm không có mức cho điểm {tron}.");
        return new DiemQuyDoi(tron, muc.DiemChu, muc.Diem4);
    }
}

internal static class KiemTraDiem
{
    public static void Thang10(decimal diem, string ten)
    {
        if (diem is < 0m or > 10m)
        {
            throw new DomainException("UPM_DIEM_NGOAI_THANG_10", $"Điểm {ten} = {diem} nằm ngoài thang 0–10.");
        }
    }
}
