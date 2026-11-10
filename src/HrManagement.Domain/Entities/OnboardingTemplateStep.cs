namespace HrManagement.Domain;

public sealed class OnboardingTemplateStep : Entity
{
    private OnboardingTemplateStep() { }

    public Guid TemplateVersionId { get; private set; }
    public string SectionTitle { get; private set; } = string.Empty;
    public int SectionOrder { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Instructions { get; private set; } = string.Empty;
    public string ExpectedOutcome { get; private set; } = string.Empty;
    public OnboardingStepType StepType { get; private set; }
    public int StepOrder { get; private set; }
    public int DueAfterDays { get; private set; }
    public bool IsRequired { get; private set; }
    public Guid? KnowledgeArticleId { get; private set; }
    public Guid? DocumentId { get; private set; }
    public string? ExternalUrl { get; private set; }
    public Guid? DependsOnStepId { get; private set; }

    public static OnboardingTemplateStep Create(Guid organizationId, Guid templateVersionId, string sectionTitle, int sectionOrder,
        string title, string instructions, string expectedOutcome, OnboardingStepType stepType, int stepOrder, int dueAfterDays,
        bool isRequired, Guid? knowledgeArticleId, Guid? documentId, string? externalUrl, Guid? dependsOnStepId)
    {
        if (dueAfterDays is < 0 or > 365) throw new DomainException("Step due offset must be between 0 and 365 days.");
        if (stepType == OnboardingStepType.ReadArticle && knowledgeArticleId is null) throw new DomainException("Article steps require a knowledge article.");
        if (stepType == OnboardingStepType.ReadDocument && documentId is null) throw new DomainException("Document steps require a document.");
        if (stepType == OnboardingStepType.ExternalResource && !IsSafeUrl(externalUrl)) throw new DomainException("External-resource steps require an HTTP or HTTPS URL.");
        return new OnboardingTemplateStep
        {
            OrganizationId = organizationId,
            TemplateVersionId = templateVersionId,
            SectionTitle = Guard.Required(sectionTitle, nameof(sectionTitle), 160),
            SectionOrder = Math.Max(0, sectionOrder),
            Title = Guard.Required(title, nameof(title), 200),
            Instructions = Guard.Required(instructions, nameof(instructions), 4_000),
            ExpectedOutcome = Guard.Required(expectedOutcome, nameof(expectedOutcome), 1_000),
            StepType = stepType,
            StepOrder = Math.Max(0, stepOrder),
            DueAfterDays = dueAfterDays,
            IsRequired = isRequired,
            KnowledgeArticleId = knowledgeArticleId,
            DocumentId = documentId,
            ExternalUrl = string.IsNullOrWhiteSpace(externalUrl) ? null : externalUrl.Trim(),
            DependsOnStepId = dependsOnStepId
        };
    }

    private static bool IsSafeUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http";
}
