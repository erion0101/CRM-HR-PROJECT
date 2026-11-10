namespace HrManagement.Domain;

public sealed class OnboardingAcknowledgment : Entity
{
    private OnboardingAcknowledgment() { }

    public Guid AssignmentId { get; private set; }
    public Guid StepProgressId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid KnowledgeArticleId { get; private set; }
    public Guid KnowledgeRevisionId { get; private set; }
    public DateTimeOffset AcknowledgedAtUtc { get; private set; }

    public static OnboardingAcknowledgment Create(Guid organizationId, Guid assignmentId, Guid stepProgressId,
        Guid employeeId, Guid knowledgeArticleId, Guid knowledgeRevisionId, DateTimeOffset now) => new()
    {
        OrganizationId = organizationId,
        AssignmentId = assignmentId,
        StepProgressId = stepProgressId,
        EmployeeId = employeeId,
        KnowledgeArticleId = knowledgeArticleId,
        KnowledgeRevisionId = knowledgeRevisionId,
        AcknowledgedAtUtc = now.ToUniversalTime()
    };
}
