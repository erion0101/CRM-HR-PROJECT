using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed record GetMyNotificationsQuery(int Limit = 30) : IQuery<NotificationListDto>;
