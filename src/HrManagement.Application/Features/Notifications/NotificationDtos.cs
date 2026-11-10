using HrManagement.Domain;

namespace HrManagement.Application.Features.Notifications;

public sealed record NotificationDto(Guid Id, NotificationType Type, string Title, string Message, string LinkPath,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? ReadAtUtc);

public sealed record NotificationListDto(IReadOnlyList<NotificationDto> Items, int UnreadCount);
