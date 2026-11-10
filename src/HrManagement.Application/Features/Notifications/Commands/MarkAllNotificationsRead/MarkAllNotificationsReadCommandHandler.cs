using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Notifications.Commands.MarkAllNotificationsRead;

public sealed class MarkAllNotificationsReadCommandHandler(INotificationRepository repository)
    : ICommandHandler<MarkAllNotificationsReadCommand, int>
{
    public Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken) =>
        repository.MarkAllMineReadAsync(DateTimeOffset.UtcNow, cancellationToken);
}
