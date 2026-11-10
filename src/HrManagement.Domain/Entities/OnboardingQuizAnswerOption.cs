namespace HrManagement.Domain;

public sealed class OnboardingQuizAnswerOption : Entity
{
    private OnboardingQuizAnswerOption() { }

    public Guid QuestionId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    [AuditRedacted] public bool IsCorrect { get; private set; }
    public int SortOrder { get; private set; }

    public static OnboardingQuizAnswerOption Create(Guid organizationId, Guid questionId, string text, bool isCorrect, int sortOrder) => new()
    {
        OrganizationId = organizationId,
        QuestionId = questionId,
        Text = Guard.Required(text, nameof(text), 1_000),
        IsCorrect = isCorrect,
        SortOrder = Math.Max(0, sortOrder)
    };
}
