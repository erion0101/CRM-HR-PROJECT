namespace HrManagement.Domain;

/// <summary>An in-app message for one employee. Links are app-relative paths, never external URLs.</summary>
public sealed class Notification : Entity
{
    private Notification() { }

    public Guid RecipientEmployeeId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string LinkPath { get; private set; } = string.Empty;
    public DateTimeOffset? ReadAtUtc { get; private set; }

    public static Notification Create(Guid organizationId, Guid recipientEmployeeId, NotificationType type, string title,
        string message, string linkPath)
    {
        var path = Guard.Required(linkPath, nameof(linkPath), 300);
        if (!path.StartsWith('/') || path.StartsWith("//", StringComparison.Ordinal))
            throw new DomainException("Notification links must be paths inside the application.");
        return new Notification
        {
            OrganizationId = organizationId,
            RecipientEmployeeId = recipientEmployeeId,
            Type = type,
            Title = Guard.Required(title, nameof(title), 200),
            Message = Guard.Required(message, nameof(message), 1_000),
            LinkPath = path
        };
    }

    public void MarkRead(DateTimeOffset now)
    {
        if (ReadAtUtc is not null) return;
        ReadAtUtc = now.ToUniversalTime();
        Touch(now);
    }
}
