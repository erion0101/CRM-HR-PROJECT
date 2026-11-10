using HrManagement.Application.Features.Notifications;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRetentionProcessor(AppDbContext db) : INotificationRetentionProcessor
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private static DateTimeOffset _nextRunUtc = DateTimeOffset.MinValue;

    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (now < _nextRunUtc) return 0;
        _nextRunUtc = now.Add(Interval);
        var readCutoff = now.AddDays(-NotificationRetention.ReadDays);
        var unreadCutoff = now.AddDays(-NotificationRetention.UnreadDays);
        return await db.Notifications
            .Where(x => (x.ReadAtUtc != null && x.CreatedAtUtc < readCutoff) || x.CreatedAtUtc < unreadCutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
