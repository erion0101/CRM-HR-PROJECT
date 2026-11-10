namespace HrManagement.Domain;

public sealed class DocumentVersion : Entity
{
    private DocumentVersion() { }
    public Guid DocumentId { get; private set; }
    public int VersionNumber { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string? Sha256 { get; private set; }
    public string? RejectionReason { get; private set; }

    public static DocumentVersion Create(Guid organizationId, Guid documentId, int number, string fileName, string storageKey, string contentType, long size) => new()
    {
        OrganizationId = organizationId,
        DocumentId = documentId,
        VersionNumber = number,
        OriginalFileName = Guard.Required(Path.GetFileName(fileName), nameof(fileName), 255),
        StorageKey = Guard.Required(storageKey, nameof(storageKey), 500),
        ContentType = Guard.Required(contentType, nameof(contentType), 100),
        SizeBytes = size
    };

    public void SetScanResult(string sha256, string? rejectionReason, DateTimeOffset now)
    {
        Sha256 = Guard.Required(sha256, nameof(sha256), 64);
        RejectionReason = rejectionReason;
        Touch(now);
    }

    public void FinalizeStorage(string storageKey, DateTimeOffset now)
    {
        StorageKey = Guard.Required(storageKey, nameof(storageKey), 500);
        Touch(now);
    }
}
