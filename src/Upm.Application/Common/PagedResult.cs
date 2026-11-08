namespace Upm.Application.Common;

/// <summary>Kết quả phân trang (query <c>page</c>, <c>pageSize</c> – CLAUDE.md §6).</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public sealed record PageRequest(int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 200;

    public int SafePage => Math.Max(1, Page);
    public int SafePageSize => Math.Clamp(PageSize, 1, MaxPageSize);
    public int Skip => (SafePage - 1) * SafePageSize;
}
