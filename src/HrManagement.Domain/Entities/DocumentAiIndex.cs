namespace HrManagement.Domain;

public sealed class DocumentAiIndex : Entity
{
    private DocumentAiIndex() { }

    public Guid DocumentId { get; private set; }
    public Guid DocumentVersionId { get; private set; }
    public DocumentAiIndexStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public string PipelineVersion { get; private set; } = "1";
    public string? EmbeddingModel { get; private set; }
    public string? ErrorCode { get; private set; }
    public DateTimeOffset? LeaseUntilUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public static DocumentAiIndex Create(Guid organizationId, Guid documentId, Guid documentVersionId, bool approved) => new()
    {
        OrganizationId = organizationId,
        DocumentId = documentId,
        DocumentVersionId = documentVersionId,
        Status = approved ? DocumentAiIndexStatus.Queued : DocumentAiIndexStatus.AwaitingApproval
    };

    public void Queue(DateTimeOffset now) { Status = DocumentAiIndexStatus.Queued; ErrorCode = null; LeaseUntilUtc = null; Touch(now); }
    public void MarkNotEligible(DateTimeOffset now) { Status = DocumentAiIndexStatus.NotEligible; LeaseUntilUtc = null; Touch(now); }

    public bool TryLease(DateTimeOffset now, TimeSpan duration)
    {
        if (Status != DocumentAiIndexStatus.Queued && !(Status == DocumentAiIndexStatus.Processing && LeaseUntilUtc < now)) return false;
        Status = DocumentAiIndexStatus.Processing;
        LeaseUntilUtc = now.Add(duration);
        Attempts++;
        Touch(now);
        return true;
    }

    public void Complete(string embeddingModel, DateTimeOffset now)
    {
        EmbeddingModel = Guard.Required(embeddingModel, nameof(embeddingModel), 100);
        Status = DocumentAiIndexStatus.Processed;
        ErrorCode = null;
        LeaseUntilUtc = null;
        ProcessedAtUtc = now;
        Touch(now);
    }

    public void NeedsOcr(DateTimeOffset now) { Status = DocumentAiIndexStatus.NeedsOcr; LeaseUntilUtc = null; Touch(now); }

    public void Fail(string errorCode, bool retry, DateTimeOffset now)
    {
        ErrorCode = Guard.Required(errorCode, nameof(errorCode), 100);
        Status = retry && Attempts < 5 ? DocumentAiIndexStatus.Queued : DocumentAiIndexStatus.Failed;
        LeaseUntilUtc = null;
        Touch(now);
    }
}
