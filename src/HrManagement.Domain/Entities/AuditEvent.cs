namespace HrManagement.Domain;

public sealed class AuditEvent
{
    private AuditEvent() { }
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid OrganizationId { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? EmployeeId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string? ChangesJson { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? IpAddress { get; private set; }

    public static AuditEvent Create(Guid organizationId, Guid? userId, Guid? employeeId, string action, string entityType, string entityId, string? changes, string? correlationId, string? ipAddress) => new()
    {
        OrganizationId = organizationId,
        UserId = userId,
        EmployeeId = employeeId,
        Action = Guard.Required(action, nameof(action), 50),
        EntityType = Guard.Required(entityType, nameof(entityType), 150),
        EntityId = Guard.Required(entityId, nameof(entityId), 100),
        ChangesJson = changes,
        CorrelationId = correlationId,
        IpAddress = ipAddress
    };
}
