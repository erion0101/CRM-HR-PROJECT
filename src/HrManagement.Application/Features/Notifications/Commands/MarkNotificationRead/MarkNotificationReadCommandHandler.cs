using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler(INotificationRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<MarkNotificationReadCommand>
{
    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await repository.GetMineAsync(request.NotificationId, cancellationToken) ?? throw new NotFoundException("Notification not found.");
        notification.MarkRead(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
