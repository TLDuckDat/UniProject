using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;

namespace UniProject.API.Modules.Notification
{
    public interface INotificationService
    {
        Task<Result<int>> CreateNotificationAsync(int? senderAccountId, CreateNotificationRequest request, CancellationToken cancellationToken = default);
        Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(int accountId, PaginationParams @params, bool? isRead = null, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(int accountId, CancellationToken cancellationToken = default);
        Task<Result> MarkAsReadAsync(int notificationRecipientId, int accountId, CancellationToken cancellationToken = default);
        Task<Result> MarkAllAsReadAsync(int accountId, CancellationToken cancellationToken = default);
    }
}
