namespace HrManagement.Domain;

public sealed class AiConversation : SoftDeletableEntity
{
    private AiConversation() { }

    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? ContextType { get; private set; }
    public Guid? ContextId { get; private set; }
    public string AuthorizationFingerprint { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public static AiConversation Create(Guid organizationId, Guid userId, string title, string? contextType, Guid? contextId, string authorizationFingerprint, DateTimeOffset expiresAtUtc) => new()
    {
        OrganizationId = organizationId,
        UserId = userId,
        Title = Guard.Required(title, nameof(title), 120),
        ContextType = string.IsNullOrWhiteSpace(contextType) ? null : Guard.Required(contextType, nameof(contextType), 40),
        ContextId = contextId,
        AuthorizationFingerprint = Guard.Required(authorizationFingerprint, nameof(authorizationFingerprint), 64),
        ExpiresAtUtc = expiresAtUtc.ToUniversalTime()
    };

    public void Refresh(DateTimeOffset now, DateTimeOffset expiresAtUtc)
    {
        ExpiresAtUtc = expiresAtUtc.ToUniversalTime();
        Touch(now);
    }
}
