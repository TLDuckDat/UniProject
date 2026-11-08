using System.Globalization;
using Upm.Domain.Common;

namespace Upm.Domain.Rules;

/// <summary>
/// Học kỳ dạng <c>"2024.1"</c> = năm học bắt đầu 2024, kỳ 1 (định dạng giả định ở Phần 2 – xem Câu hỏi cho BA).
/// Kỳ 1, 2 là kỳ chính; kỳ 3 (hè) được chấp nhận khi đọc nhưng không phải "học kỳ liền kề".
/// </summary>
public readonly record struct HocKy : IComparable<HocKy>
{
    public HocKy(int namHoc, int ky)
    {
        if (namHoc is < 1900 or > 2999 || ky is < 1 or > 3)
        {
            throw new DomainException("UPM_HOC_KY_KHONG_HOP_LE", $"Học kỳ {namHoc}.{ky} không hợp lệ.");
        }

        NamHoc = namHoc;
        Ky = ky;
    }

    public int NamHoc { get; }

    public int Ky { get; }

    public static HocKy Parse(string giaTri)
    {
        var parts = (giaTri ?? string.Empty).Split('.');
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var nam)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var ky))
        {
            throw new DomainException("UPM_HOC_KY_KHONG_HOP_LE", $"Mã học kỳ \"{giaTri}\" không đúng dạng NNNN.K.");
        }

        return new HocKy(nam, ky);
    }

    /// <summary>Học kỳ chính liền kề (dùng cho hạn HOAN_BAO_VE – D01): 2024.1 → 2024.2; 2024.2 và 2024.3 → 2025.1.</summary>
    public HocKy KeTiep() => Ky == 1 ? new HocKy(NamHoc, 2) : new HocKy(NamHoc + 1, 1);

    public int CompareTo(HocKy other) =>
        NamHoc != other.NamHoc ? NamHoc.CompareTo(other.NamHoc) : Ky.CompareTo(other.Ky);

    public static bool operator <(HocKy a, HocKy b) => a.CompareTo(b) < 0;

    public static bool operator >(HocKy a, HocKy b) => a.CompareTo(b) > 0;

    public static bool operator <=(HocKy a, HocKy b) => a.CompareTo(b) <= 0;

    public static bool operator >=(HocKy a, HocKy b) => a.CompareTo(b) >= 0;

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{NamHoc}.{Ky}");
}

/// <summary>Lịch học kỳ (ngày kết thúc từng kỳ) – Application cài đặt từ dữ liệu QLĐT/cấu hình.</summary>
public interface ILichHocKy
{
    DateOnly NgayKetThuc(HocKy hocKy);
}
