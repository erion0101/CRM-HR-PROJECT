using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Notifications.Commands.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : ICommand;
