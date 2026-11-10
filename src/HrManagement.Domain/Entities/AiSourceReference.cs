namespace HrManagement.Domain;

public sealed class AiSourceReference : Entity
{
    private AiSourceReference() { }

    public Guid MessageId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public Guid? EmployeeId { get; private set; }
    public Guid? DocumentId { get; private set; }
    public Guid? DocumentVersionId { get; private set; }
    public Guid? DocumentChunkId { get; private set; }
    public string? Location { get; private set; }
    public string? Url { get; private set; }

    public static AiSourceReference Create(Guid organizationId, Guid messageId, string kind, string label, string? entityType, Guid? entityId, Guid? employeeId, Guid? documentId, Guid? documentVersionId, Guid? documentChunkId, string? location, string? url) => new()
    {
        OrganizationId = organizationId,
        MessageId = messageId,
        Kind = Guard.Required(kind, nameof(kind), 40),
        Label = Guard.Required(label, nameof(label), 200),
        EntityType = entityType,
        EntityId = entityId,
        EmployeeId = employeeId,
        DocumentId = documentId,
        DocumentVersionId = documentVersionId,
        DocumentChunkId = documentChunkId,
        Location = location,
        Url = url
    };
}
