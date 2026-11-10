using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsQueryHandler(INotificationRepository repository) : IQueryHandler<GetMyNotificationsQuery, NotificationListDto>
{
    public Task<NotificationListDto> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken) =>
        repository.ListMineAsync(Math.Clamp(request.Limit, 1, 100), cancellationToken);
}
