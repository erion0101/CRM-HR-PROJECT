namespace HrManagement.Domain;

public sealed class EmployeeDocument : SoftDeletableEntity
{
    private EmployeeDocument() { }
    public string DisplayName { get; private set; } = string.Empty;
    public string DocumentType { get; private set; } = string.Empty;
    public Guid? FolderId { get; private set; }
    public DocumentStatus Status { get; private set; } = DocumentStatus.PendingScan;
    public int CurrentVersion { get; private set; } = 1;
    public DataClassification DataClassification { get; private set; } = DataClassification.Sensitive;
    public bool AiIndexingApproved { get; private set; }
    public Guid? AiIndexingApprovedBy { get; private set; }
    public DateTimeOffset? AiIndexingApprovedAtUtc { get; private set; }

    public static EmployeeDocument Create(Guid organizationId, string displayName, string documentType, Guid? folderId = null) => new()
    {
        OrganizationId = organizationId,
        DisplayName = Guard.Required(displayName, nameof(displayName), 255),
        DocumentType = Guard.Required(documentType, nameof(documentType), 80),
        FolderId = folderId
    };

    public void AddVersion(DateTimeOffset now)
    {
        CurrentVersion++;
        Status = DocumentStatus.PendingScan;
        AiIndexingApproved = false;
        AiIndexingApprovedBy = null;
        AiIndexingApprovedAtUtc = null;
        Touch(now);
    }
    public void MarkClean(DateTimeOffset now) { Status = DocumentStatus.Clean; Touch(now); }
    public void Reject(DateTimeOffset now) { Status = DocumentStatus.Rejected; Touch(now); }

    public void ApproveAiIndexing(DataClassification classification, Guid approvedBy, DateTimeOffset now)
    {
        if (classification is DataClassification.Sensitive or DataClassification.HighlyRestricted)
            throw new DomainException("Sensitive documents cannot be approved for AI indexing.");
        if (Status != DocumentStatus.Clean) throw new DomainException("Only clean documents can be approved for AI indexing.");
        DataClassification = classification;
        AiIndexingApproved = true;
        AiIndexingApprovedBy = approvedBy;
        AiIndexingApprovedAtUtc = now.ToUniversalTime();
        Touch(now);
    }

    public void RevokeAiIndexing(DateTimeOffset now)
    {
        AiIndexingApproved = false;
        AiIndexingApprovedBy = null;
        AiIndexingApprovedAtUtc = null;
        Touch(now);
    }
}
