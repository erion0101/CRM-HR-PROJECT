namespace HrManagement.Domain;

public sealed class OnboardingQuizQuestion : Entity
{
    private OnboardingQuizQuestion() { }

    public Guid QuizId { get; private set; }
    public string QuestionText { get; private set; } = string.Empty;
    [AuditRedacted] public string Explanation { get; private set; } = string.Empty;
    public string Topic { get; private set; } = string.Empty;
    public OnboardingQuizDifficulty Difficulty { get; private set; }
    public int SortOrder { get; private set; }
    public Guid? SourceDocumentId { get; private set; }
    public Guid? SourceDocumentVersionId { get; private set; }
    public Guid? SourceKnowledgeRevisionId { get; private set; }
    public string SourceLabel { get; private set; } = string.Empty;
    public int? SourcePage { get; private set; }
    public int? SourceParagraph { get; private set; }

    public static OnboardingQuizQuestion Create(Guid organizationId, Guid quizId, string questionText, string explanation,
        string topic, OnboardingQuizDifficulty difficulty, int sortOrder, Guid? sourceDocumentId,
        Guid? sourceDocumentVersionId, Guid? sourceKnowledgeRevisionId, string sourceLabel, int? sourcePage, int? sourceParagraph) => new()
    {
        OrganizationId = organizationId,
        QuizId = quizId,
        QuestionText = Guard.Required(questionText, nameof(questionText), 2_000),
        Explanation = Guard.Required(explanation, nameof(explanation), 4_000),
        Topic = Guard.Required(topic, nameof(topic), 160),
        Difficulty = difficulty,
        SortOrder = Math.Max(0, sortOrder),
        SourceDocumentId = sourceDocumentId,
        SourceDocumentVersionId = sourceDocumentVersionId,
        SourceKnowledgeRevisionId = sourceKnowledgeRevisionId,
        SourceLabel = Guard.Required(sourceLabel, nameof(sourceLabel), 240),
        SourcePage = sourcePage,
        SourceParagraph = sourceParagraph
    };

    public void Update(string questionText, string explanation, string topic, OnboardingQuizDifficulty difficulty,
        int sortOrder, Guid? sourceDocumentId, Guid? sourceDocumentVersionId, Guid? sourceKnowledgeRevisionId,
        string sourceLabel, int? sourcePage, int? sourceParagraph, DateTimeOffset now)
    {
        QuestionText = Guard.Required(questionText, nameof(questionText), 2_000);
        Explanation = Guard.Required(explanation, nameof(explanation), 4_000);
        Topic = Guard.Required(topic, nameof(topic), 160);
        Difficulty = difficulty;
        SortOrder = Math.Max(0, sortOrder);
        SourceDocumentId = sourceDocumentId;
        SourceDocumentVersionId = sourceDocumentVersionId;
        SourceKnowledgeRevisionId = sourceKnowledgeRevisionId;
        SourceLabel = Guard.Required(sourceLabel, nameof(sourceLabel), 240);
        SourcePage = sourcePage;
        SourceParagraph = sourceParagraph;
        Touch(now);
    }
}
