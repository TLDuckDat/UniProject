using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Exceptions;
using UniProject.Share.Infrastructure.Extensions;
using UniProject.API.Data;

namespace UniProject.API.Modules.Notification
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(AppDbContext dbContext, ILogger<NotificationService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Mỗi người nhận có một bản ghi NguoiNhanThongBao riêng để theo dõi trạng thái đã đọc.
        /// </summary>
        public async Task<Result<int>> CreateNotificationAsync(int? senderAccountId, CreateNotificationRequest request, CancellationToken cancellationToken = default)
        {
            var priority = request.Priority.ToUpperInvariant() switch
            {
                "QUAN_TRONG" => NotificationPriority.QUAN_TRONG,
                "KHAN_CAP" => NotificationPriority.KHAN_CAP,
                _ => NotificationPriority.BINH_THUONG
            };

            var notification = new ThongBao(request.Title, request.Content, request.NotificationType, priority, senderAccountId);
            _dbContext.ThongBaos.Add(notification);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (request.RecipientAccountIds != null && request.RecipientAccountIds.Any())
            {
                foreach (var accountId in request.RecipientAccountIds.Distinct())
                {
                    notification.AddRecipient(accountId);
                }
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Notification {Id} created by {SenderId}", notification.Id, senderAccountId);
            return Result<int>.Ok(notification.Id, "Tạo thông báo thành công.");
        }

        public async Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(int accountId, PaginationParams @params, bool? isRead = null, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.NguoiNhanThongBaos
                .AsNoTracking()
                .Include(x => x.ThongBao)
                .Where(x => x.MaTaiKhoan == accountId);

            if (isRead.HasValue)
                query = query.Where(x => x.DaDoc == isRead.Value);

            query = query.OrderByDescending(x => x.ThongBao.NgayTao);

            return await query.ToPagedResultAsync(
                @params.PageNumber,
                @params.PageSize,
                x => new NotificationDto(
                    x.Id,
                    x.ThongBao.TieuDe,
                    x.ThongBao.NoiDung,
                    x.ThongBao.LoaiThongBao,
                    x.ThongBao.MucDo.ToString().ToUpperInvariant(),
                    x.ThongBao.NgayTao,
                    x.DaDoc,
                    x.NgayDoc),
                cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(int accountId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.NguoiNhanThongBaos
                .CountAsync(x => x.MaTaiKhoan == accountId && !x.DaDoc, cancellationToken);
        }

        /// <summary>
        /// Lọc theo accountId để người dùng không đánh dấu được thông báo của người khác.
        /// </summary>
        public async Task<Result> MarkAsReadAsync(int notificationRecipientId, int accountId, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.NguoiNhanThongBaos
                .FirstOrDefaultAsync(x => x.Id == notificationRecipientId && x.MaTaiKhoan == accountId, cancellationToken);

            if (item == null) throw new NotFoundException("Thông báo", notificationRecipientId);

            item.MarkAsRead();
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok("Đã đánh dấu đã đọc.");
        }

        public async Task<Result> MarkAllAsReadAsync(int accountId, CancellationToken cancellationToken = default)
        {
            var unread = await _dbContext.NguoiNhanThongBaos
                .Where(x => x.MaTaiKhoan == accountId && !x.DaDoc)
                .ToListAsync(cancellationToken);

            foreach (var item in unread)
            {
                item.MarkAsRead();
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok("Đã đánh dấu tất cả là đã đọc.");
        }
    }
}
