namespace UniProject.Share.Application.Pagination
{
    /// <summary>
    /// Đại diện cho một trang dữ liệu cùng với siêu dữ liệu phân trang.
    /// </summary>
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalItems { get; init; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 0;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
        public PagedResult() { }
        public PagedResult(IReadOnlyList<T> items, int totalItems, int pageNumber, int pageSize)
        {
            Items = items;
            TotalItems = totalItems;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
