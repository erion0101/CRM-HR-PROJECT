namespace HrManagement.Domain;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
    public Guid OrganizationId { get; protected set; }
    public DateTimeOffset CreatedAtUtc { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; protected set; } = DateTimeOffset.UtcNow;
    public long Version { get; protected set; } = 1;

    public void Touch(DateTimeOffset now)
    {
        UpdatedAtUtc = now.ToUniversalTime();
        Version++;
    }
}
