namespace HrManagement.Application.Features.AuditEvents;

public sealed record AuditEventDto(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    Guid? UserId,
    Guid? EmployeeId,
    string? ActorEmail,
    string Action,
    string EntityType,
    string EntityId,
    string? CorrelationId,
    string? IpAddress);
