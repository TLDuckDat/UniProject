namespace Upm.Domain.Rules;

/// <summary>
/// Helper làm tròn DUY NHẤT của hệ thống (CLAUDE.md §5.1): chỉ <c>decimal</c>, half-up
/// (<see cref="MidpointRounding.AwayFromZero"/>). Đặc tả mục 14: float có thể biểu diễn 1,895 thành 1,89499…
/// và đánh trượt oan – vì vậy không có overload double.
/// </summary>
public static class LamTron
{
    /// <summary>GPA: 2 chữ số thập phân (1,895 → 1,90; 1,8949 → 1,89).</summary>
    public static decimal Gpa(decimal gpa) => Math.Round(gpa, 2, MidpointRounding.AwayFromZero);

    /// <summary>Điểm thành phần, điểm bảo vệ, điểm ĐATN: 1 chữ số thập phân (5,55 → 5,6; 7,25 → 7,3).</summary>
    public static decimal Diem(decimal diem) => Math.Round(diem, 1, MidpointRounding.AwayFromZero);
}
