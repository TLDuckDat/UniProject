namespace UniProject.API.Modules.Notification
{
    public record NotificationDto(
        int Id,
        string Title,
        string Content,
        string? NotificationType,
        string Priority,
        DateTime CreatedDate,
        bool IsRead,
        DateTime? ReadDate);

    public record CreateNotificationRequest(
        string Title,
        string Content,
        string? NotificationType,
        string Priority = "BINH_THUONG",
        IReadOnlyList<int>? RecipientAccountIds = null);

    public record SendToUserRequest(int AccountId, string Title, string Content, string? NotificationType, string Priority = "BINH_THUONG");
}
