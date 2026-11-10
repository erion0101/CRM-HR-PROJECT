namespace HrManagement.Domain;

public sealed class OnboardingReadingCycle : Entity
{
    private OnboardingReadingCycle() { }

    public Guid AssignmentId { get; private set; }
    public int CycleNumber { get; private set; }
    public OnboardingReadingCycleStatus Status { get; private set; } = OnboardingReadingCycleStatus.Active;
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }
    public Guid? FailedAttemptId { get; private set; }

    public static OnboardingReadingCycle Create(Guid organizationId, Guid assignmentId, int cycleNumber, DateTimeOffset now)
    {
        if (cycleNumber < 1) throw new DomainException("Reading cycle number must be positive.");
        return new OnboardingReadingCycle
        {
            OrganizationId = organizationId,
            AssignmentId = assignmentId,
            CycleNumber = cycleNumber,
            StartedAtUtc = now.ToUniversalTime()
        };
    }

    public void Fail(Guid attemptId, DateTimeOffset now)
    {
        if (Status != OnboardingReadingCycleStatus.Active) throw new DomainException("Only an active reading cycle can fail.");
        Status = OnboardingReadingCycleStatus.Failed;
        FailedAttemptId = attemptId;
        EndedAtUtc = now.ToUniversalTime();
        Touch(now);
    }

    public void Pass(DateTimeOffset now)
    {
        if (Status != OnboardingReadingCycleStatus.Active) throw new DomainException("Only an active reading cycle can pass.");
        Status = OnboardingReadingCycleStatus.Passed;
        EndedAtUtc = now.ToUniversalTime();
        Touch(now);
    }
}
