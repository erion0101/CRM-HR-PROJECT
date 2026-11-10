using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Notifications;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository(AppDbContext db, ICurrentUserService currentUser) : INotificationRepository
{
    private Guid OrganizationId => currentUser.OrganizationId ?? throw new ForbiddenException();

    public void Add(Notification notification) => db.Notifications.Add(notification);

    public async Task<NotificationListDto> ListMineAsync(int limit, CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is not { } employeeId) return new NotificationListDto([], 0);
        var mine = Mine(employeeId);
        var items = await mine.OrderByDescending(x => x.CreatedAtUtc).Take(limit)
            .Select(x => new NotificationDto(x.Id, x.Type, x.Title, x.Message, x.LinkPath, x.CreatedAtUtc, x.ReadAtUtc))
            .ToListAsync(cancellationToken);
        var unread = await mine.CountAsync(x => x.ReadAtUtc == null, cancellationToken);
        return new NotificationListDto(items, unread);
    }

    public Task<Notification?> GetMineAsync(Guid id, CancellationToken cancellationToken) =>
        currentUser.EmployeeId is { } employeeId
            ? db.Notifications.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.RecipientEmployeeId == employeeId && x.Id == id, cancellationToken)
            : Task.FromResult<Notification?>(null);

    /// <summary>Bulk update: reading notifications is not business history, so it bypasses per-row auditing.</summary>
    public Task<int> MarkAllMineReadAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        currentUser.EmployeeId is { } employeeId
            ? Mine(employeeId).Where(x => x.ReadAtUtc == null).ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.ReadAtUtc, now.ToUniversalTime())
                .SetProperty(x => x.UpdatedAtUtc, now.ToUniversalTime())
                .SetProperty(x => x.Version, x => x.Version + 1), cancellationToken)
            : Task.FromResult(0);

    private IQueryable<Notification> Mine(Guid employeeId) =>
        db.Notifications.AsNoTracking().Where(x => x.OrganizationId == OrganizationId && x.RecipientEmployeeId == employeeId);
}
