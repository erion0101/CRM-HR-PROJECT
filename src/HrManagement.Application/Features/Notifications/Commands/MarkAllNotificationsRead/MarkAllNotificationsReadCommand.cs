using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Notifications.Commands.MarkAllNotificationsRead;

public sealed record MarkAllNotificationsReadCommand : ICommand<int>;
