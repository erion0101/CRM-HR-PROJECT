namespace HrManagement.Domain;

public sealed class DocumentAssociation : Entity
{
    private DocumentAssociation() { }
    public Guid DocumentId { get; private set; }
    public DocumentAssociationType AssociationType { get; private set; }
    public Guid AssociatedEntityId { get; private set; }

    public static DocumentAssociation Create(Guid organizationId, Guid documentId, DocumentAssociationType type, Guid entityId) => new()
    {
        OrganizationId = organizationId,
        DocumentId = documentId,
        AssociationType = type,
        AssociatedEntityId = entityId
    };
}
