namespace HrManagement.Domain;

public sealed class UserScope
{
    private UserScope() { }
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public ScopeType ScopeType { get; private set; }
    public Guid? ScopeEntityId { get; private set; }

    public static UserScope Create(Guid organizationId, Guid userId, ScopeType type, Guid? entityId = null) => new()
    {
        OrganizationId = organizationId,
        UserId = userId,
        ScopeType = type,
        ScopeEntityId = entityId
    };
}
