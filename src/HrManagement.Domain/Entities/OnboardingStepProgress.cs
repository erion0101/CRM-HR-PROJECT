namespace HrManagement.Domain;

public sealed class OnboardingStepProgress : Entity
{
    private OnboardingStepProgress() { }

    public Guid AssignmentId { get; private set; }
    public Guid TemplateStepId { get; private set; }
    public string SectionTitle { get; private set; } = string.Empty;
    public int SectionOrder { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Instructions { get; private set; } = string.Empty;
    public string ExpectedOutcome { get; private set; } = string.Empty;
    public OnboardingStepType StepType { get; private set; }
    public int StepOrder { get; private set; }
    public bool IsRequired { get; private set; }
    public DateOnly DueDate { get; private set; }
    public Guid? KnowledgeArticleId { get; private set; }
    public Guid? KnowledgeRevisionId { get; private set; }
    public Guid? DocumentId { get; private set; }
    public string? ExternalUrl { get; private set; }
    public Guid? DependsOnTemplateStepId { get; private set; }
    public int? MinimumReadingMinutes { get; private set; }
    public OnboardingStepStatus Status { get; private set; } = OnboardingStepStatus.NotStarted;
    public string? CompletionNote { get; private set; }
    public Guid? CompletedByUserId { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? ReviewNote { get; private set; }

    public static OnboardingStepProgress Create(Guid organizationId, Guid assignmentId, OnboardingTemplateStep step,
        DateOnly startDate, Guid? knowledgeRevisionId, int sectionOrderOffset = 0) => new()
    {
        OrganizationId = organizationId,
        AssignmentId = assignmentId,
        TemplateStepId = step.Id,
        SectionTitle = step.SectionTitle,
        SectionOrder = step.SectionOrder + Math.Max(0, sectionOrderOffset),
        Title = step.Title,
        Instructions = step.Instructions,
        ExpectedOutcome = step.ExpectedOutcome,
        StepType = step.StepType,
        StepOrder = step.StepOrder,
        IsRequired = step.IsRequired,
        DueDate = startDate.AddDays(step.DueAfterDays),
        KnowledgeArticleId = step.KnowledgeArticleId,
        KnowledgeRevisionId = knowledgeRevisionId,
        DocumentId = step.DocumentId,
        ExternalUrl = step.ExternalUrl,
        DependsOnTemplateStepId = step.DependsOnStepId,
        MinimumReadingMinutes = step.MinimumReadingMinutes
    };

    public void SetProgress(OnboardingStepStatus status, string? note, Guid actorUserId, bool dependencyCompleted, DateTimeOffset now)
    {
        if (!dependencyCompleted && status is OnboardingStepStatus.Completed or OnboardingStepStatus.PendingReview)
            throw new DomainException("The required previous onboarding step is not complete.");
        if (StepType == OnboardingStepType.ManagerReviewedMilestone && status == OnboardingStepStatus.Completed)
            status = OnboardingStepStatus.PendingReview;
        Status = status;
        CompletionNote = string.IsNullOrWhiteSpace(note) ? null : Guard.Required(note, nameof(note), 2_000);
        CompletedByUserId = status is OnboardingStepStatus.Completed or OnboardingStepStatus.PendingReview ? actorUserId : null;
        CompletedAtUtc = status is OnboardingStepStatus.Completed or OnboardingStepStatus.PendingReview ? now.ToUniversalTime() : null;
        if (status != OnboardingStepStatus.Completed) { ReviewedByUserId = null; ReviewedAtUtc = null; ReviewNote = null; }
        Touch(now);
    }

    public void Review(bool approved, Guid reviewerUserId, string? note, DateTimeOffset now)
    {
        if (StepType != OnboardingStepType.ManagerReviewedMilestone || Status != OnboardingStepStatus.PendingReview)
            throw new DomainException("This onboarding step is not awaiting manager review.");
        Status = approved ? OnboardingStepStatus.Completed : OnboardingStepStatus.InProgress;
        ReviewedByUserId = reviewerUserId;
        ReviewedAtUtc = now.ToUniversalTime();
        ReviewNote = string.IsNullOrWhiteSpace(note) ? null : Guard.Required(note, nameof(note), 2_000);
        Touch(now);
    }

    public void ResetForReadingCycle(DateTimeOffset now)
    {
        if (StepType is not (OnboardingStepType.ReadDocument or OnboardingStepType.ReadArticle or OnboardingStepType.PolicyAcknowledgment)) return;
        Status = OnboardingStepStatus.NotStarted;
        CompletionNote = null;
        CompletedByUserId = null;
        CompletedAtUtc = null;
        ReviewedByUserId = null;
        ReviewedAtUtc = null;
        ReviewNote = null;
        Touch(now);
    }
}
