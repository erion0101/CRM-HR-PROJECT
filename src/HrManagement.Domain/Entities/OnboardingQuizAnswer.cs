namespace HrManagement.Domain;

public sealed class OnboardingQuizAnswer : Entity
{
    private OnboardingQuizAnswer() { }

    public Guid AttemptId { get; private set; }
    public Guid QuestionId { get; private set; }
    public Guid SelectedOptionId { get; private set; }
    [AuditRedacted] public bool IsCorrect { get; private set; }

    public static OnboardingQuizAnswer Create(Guid organizationId, Guid attemptId, Guid questionId, Guid selectedOptionId, bool isCorrect) => new()
    {
        OrganizationId = organizationId,
        AttemptId = attemptId,
        QuestionId = questionId,
        SelectedOptionId = selectedOptionId,
        IsCorrect = isCorrect
    };
}
