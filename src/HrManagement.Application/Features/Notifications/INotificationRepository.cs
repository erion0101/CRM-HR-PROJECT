using HrManagement.Domain;

namespace HrManagement.Application.Features.Notifications;

/// <summary>Notifications for the signed-in user's own employee record only.</summary>
public interface INotificationRepository
{
    void Add(Notification notification);
    Task<NotificationListDto> ListMineAsync(int limit, CancellationToken cancellationToken);
    Task<Notification?> GetMineAsync(Guid id, CancellationToken cancellationToken);
    Task<int> MarkAllMineReadAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
