namespace HrManagement.Application.Features.Notifications;

/// <summary>Worker job that deletes old notifications so the table does not grow forever.</summary>
public interface INotificationRetentionProcessor
{
    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken);
}

public static class NotificationRetention
{
    /// <summary>Read notifications are kept this long after they were created.</summary>
    public const int ReadDays = 90;
    /// <summary>Unread notifications are kept longer, so nothing disappears before someone has had a chance to see it.</summary>
    public const int UnreadDays = 180;
}
