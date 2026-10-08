namespace UniProject.Share.Application.Pagination
{
    public class PaginationParams
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 20;
        private int _pageNumber = 1;

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = Math.Max(1, value);
        }
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 1 : value);
        }
        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public string SortDirection { get; set; } = "asc"; // "asc" or "desc"
    }
}
