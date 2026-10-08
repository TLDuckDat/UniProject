namespace UniProject.Share.Application.Interfaces
{
    /// <summary>
    /// Cung cấp thông tin về người dùng hiện đang được xác thực.
    /// </summary>
    public interface ICurrentUser
    {
        /// <summary>
        /// Lấy mã định danh của người dùng hiện tại, hoặc null nếu chưa được xác thực. 
        /// </summary>
        int? UserID { get; }
        string? Username { get; }
        IReadOnlyList<string> Roles { get; }
        bool IsAuthenticated { get; }
        bool IsInRole(string role);
    }
}
