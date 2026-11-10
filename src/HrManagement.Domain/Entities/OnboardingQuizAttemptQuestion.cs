namespace HrManagement.Domain;

public sealed class OnboardingQuizAttemptQuestion : Entity
{
    private OnboardingQuizAttemptQuestion() { }

    public Guid AttemptId { get; private set; }
    public Guid QuestionId { get; private set; }
    public int SortOrder { get; private set; }
    public string OptionOrder { get; private set; } = string.Empty;

    public static OnboardingQuizAttemptQuestion Create(Guid organizationId, Guid attemptId, Guid questionId, int sortOrder,
        IEnumerable<Guid> optionOrder) => new()
    {
        OrganizationId = organizationId,
        AttemptId = attemptId,
        QuestionId = questionId,
        SortOrder = sortOrder,
        OptionOrder = string.Join(',', optionOrder)
    };

    public IReadOnlyList<Guid> GetOptionOrder() => OptionOrder.Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(Guid.Parse).ToArray();
}
