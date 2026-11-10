namespace HrManagement.Domain;

public sealed class OnboardingReadingProgress : Entity
{
    private OnboardingReadingProgress() { }

    public Guid ReadingCycleId { get; private set; }
    public Guid StepProgressId { get; private set; }
    public Guid? DocumentId { get; private set; }
    public Guid? DocumentVersionId { get; private set; }
    public Guid? KnowledgeArticleId { get; private set; }
    public Guid? KnowledgeRevisionId { get; private set; }
    public bool IsRequired { get; private set; }
    public OnboardingReadingStatus Status { get; private set; } = OnboardingReadingStatus.Unread;
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }

    public static OnboardingReadingProgress Create(Guid organizationId, Guid cycleId, OnboardingStepProgress step, Guid? documentVersionId)
    {
        if (step.StepType is not (OnboardingStepType.ReadDocument or OnboardingStepType.ReadArticle or OnboardingStepType.PolicyAcknowledgment))
            throw new DomainException("Only reading steps can be included in a reading cycle.");
        return new OnboardingReadingProgress
        {
            OrganizationId = organizationId,
            ReadingCycleId = cycleId,
            StepProgressId = step.Id,
            DocumentId = step.DocumentId,
            DocumentVersionId = documentVersionId,
            KnowledgeArticleId = step.KnowledgeArticleId,
            KnowledgeRevisionId = step.KnowledgeRevisionId,
            IsRequired = step.IsRequired
        };
    }

    public void Start(DateTimeOffset now)
    {
        if (Status == OnboardingReadingStatus.Read) return;
        Status = OnboardingReadingStatus.InProgress;
        StartedAtUtc ??= now.ToUniversalTime();
        Touch(now);
    }

    public bool HasReadableSource => DocumentId is not null || KnowledgeArticleId is not null;

    public void MarkRead(DateTimeOffset now)
    {
        if (Status == OnboardingReadingStatus.Read) return;
        if (Status == OnboardingReadingStatus.Unread && HasReadableSource)
            throw new DomainException("Open the document before marking it as read.");
        Status = OnboardingReadingStatus.Read;
        StartedAtUtc ??= now.ToUniversalTime();
        ReadAtUtc = now.ToUniversalTime();
        Touch(now);
    }
}
