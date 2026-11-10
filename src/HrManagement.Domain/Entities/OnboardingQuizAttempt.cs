namespace HrManagement.Domain;

public sealed class OnboardingQuizAttempt : Entity
{
    private OnboardingQuizAttempt() { }

    public Guid AssignmentId { get; private set; }
    public Guid ReadingCycleId { get; private set; }
    public Guid QuizId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public int AttemptNumber { get; private set; }
    public OnboardingQuizAttemptStatus Status { get; private set; } = OnboardingQuizAttemptStatus.InProgress;
    public int PassingPercentage { get; private set; }
    public int? CorrectAnswers { get; private set; }
    public int? TotalQuestions { get; private set; }
    public decimal? ScorePercentage { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }

    public static OnboardingQuizAttempt Create(Guid organizationId, Guid assignmentId, Guid readingCycleId, Guid quizId,
        Guid employeeId, int attemptNumber, int passingPercentage, DateTimeOffset now) => new()
    {
        OrganizationId = organizationId,
        AssignmentId = assignmentId,
        ReadingCycleId = readingCycleId,
        QuizId = quizId,
        EmployeeId = employeeId,
        AttemptNumber = attemptNumber,
        PassingPercentage = passingPercentage,
        StartedAtUtc = now.ToUniversalTime()
    };

    public bool Submit(int correct, int total, DateTimeOffset now)
    {
        if (Status != OnboardingQuizAttemptStatus.InProgress) throw new DomainException("This quiz attempt has already been submitted.");
        if (total < 1 || correct < 0 || correct > total) throw new DomainException("The quiz result is invalid.");
        CorrectAnswers = correct;
        TotalQuestions = total;
        ScorePercentage = Math.Round(correct * 100m / total, 2);
        Status = ScorePercentage >= PassingPercentage ? OnboardingQuizAttemptStatus.Passed : OnboardingQuizAttemptStatus.Failed;
        SubmittedAtUtc = now.ToUniversalTime();
        Touch(now);
        return Status == OnboardingQuizAttemptStatus.Passed;
    }
}
