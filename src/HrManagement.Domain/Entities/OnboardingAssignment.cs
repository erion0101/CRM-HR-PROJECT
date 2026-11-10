namespace HrManagement.Domain;

public sealed class OnboardingAssignment : Entity
{
    private OnboardingAssignment() { }

    public Guid EmployeeId { get; private set; }
    public Guid TemplateId { get; private set; }
    public Guid TemplateVersionId { get; private set; }
    public int TemplateVersionNumber { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? ProjectTemplateId { get; private set; }
    public Guid? ProjectTemplateVersionId { get; private set; }
    public int? ProjectTemplateVersionNumber { get; private set; }
    public Guid? ManagerEmployeeId { get; private set; }
    public Guid? MentorEmployeeId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public OnboardingAssignmentStatus Status { get; private set; } = OnboardingAssignmentStatus.NotStarted;
    public OnboardingAssessmentStatus AssessmentStatus { get; private set; } = OnboardingAssessmentStatus.NotConfigured;
    public Guid? CurrentReadingCycleId { get; private set; }
    public int CurrentReadingCycleNumber { get; private set; }
    public DateTimeOffset? AssessmentPassedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }

    public static OnboardingAssignment Create(Guid organizationId, Guid employeeId, Guid templateId, Guid templateVersionId,
        int templateVersionNumber, Guid? projectId, Guid? managerEmployeeId, Guid? mentorEmployeeId, DateOnly startDate) => new()
    {
        OrganizationId = organizationId,
        EmployeeId = employeeId,
        TemplateId = templateId,
        TemplateVersionId = templateVersionId,
        TemplateVersionNumber = templateVersionNumber,
        ProjectId = projectId,
        ManagerEmployeeId = managerEmployeeId,
        MentorEmployeeId = mentorEmployeeId,
        StartDate = startDate
    };

    /// <summary>Template versions whose steps and question banks make up this journey, company package first.</summary>
    public IReadOnlyList<Guid> PackageVersionIds => ProjectTemplateVersionId is { } projectVersionId
        ? [TemplateVersionId, projectVersionId]
        : [TemplateVersionId];

    public void IncludeProjectPackage(Guid templateId, Guid templateVersionId, int templateVersionNumber)
    {
        if (ProjectId is null) throw new DomainException("A project package requires a project on the onboarding journey.");
        if (ProjectTemplateId is not null) throw new DomainException("This onboarding journey already includes a project package.");
        if (templateId == TemplateId) throw new DomainException("The project package must differ from the main onboarding package.");
        if (Status != OnboardingAssignmentStatus.NotStarted) throw new DomainException("A project package can only be added before the journey starts.");
        ProjectTemplateId = templateId;
        ProjectTemplateVersionId = templateVersionId;
        ProjectTemplateVersionNumber = templateVersionNumber;
    }

    public void ConfigureAssessment(Guid readingCycleId, int cycleNumber, bool requiredReadingComplete, DateTimeOffset now)
    {
        CurrentReadingCycleId = readingCycleId;
        CurrentReadingCycleNumber = cycleNumber;
        AssessmentStatus = requiredReadingComplete ? OnboardingAssessmentStatus.Available : OnboardingAssessmentStatus.Locked;
        Touch(now);
    }

    public void RefreshAssessmentAvailability(bool allRequiredReadingComplete, DateTimeOffset now)
    {
        if (AssessmentStatus is OnboardingAssessmentStatus.NotConfigured or OnboardingAssessmentStatus.Passed) return;
        AssessmentStatus = allRequiredReadingComplete ? OnboardingAssessmentStatus.Available : OnboardingAssessmentStatus.Locked;
        Touch(now);
    }

    public void StartAssessment(DateTimeOffset now)
    {
        if (AssessmentStatus != OnboardingAssessmentStatus.Available) throw new DomainException("The assessment is locked until all required reading is complete.");
        AssessmentStatus = OnboardingAssessmentStatus.InProgress;
        Touch(now);
    }

    public void RestartAssessment(Guid nextReadingCycleId, int cycleNumber, DateTimeOffset now)
    {
        CurrentReadingCycleId = nextReadingCycleId;
        CurrentReadingCycleNumber = cycleNumber;
        AssessmentStatus = OnboardingAssessmentStatus.Locked;
        AssessmentPassedAtUtc = null;
        Status = OnboardingAssignmentStatus.InProgress;
        CompletedAtUtc = null;
        Touch(now);
    }

    public void PassAssessment(DateTimeOffset now)
    {
        AssessmentStatus = OnboardingAssessmentStatus.Passed;
        AssessmentPassedAtUtc = now.ToUniversalTime();
        Touch(now);
    }

    public void RefreshStatus(bool hasProgress, bool hasBlocked, bool hasPendingReview, bool allRequiredCompleted, DateTimeOffset now)
    {
        if (Status is OnboardingAssignmentStatus.Cancelled or OnboardingAssignmentStatus.Superseded) return;
        var assessmentComplete = AssessmentStatus is OnboardingAssessmentStatus.NotConfigured or OnboardingAssessmentStatus.Passed;
        Status = allRequiredCompleted && assessmentComplete
            ? OnboardingAssignmentStatus.Completed
            : hasBlocked
                ? OnboardingAssignmentStatus.Blocked
                : hasPendingReview
                    ? OnboardingAssignmentStatus.PendingReview
                    : hasProgress ? OnboardingAssignmentStatus.InProgress : OnboardingAssignmentStatus.NotStarted;
        CompletedAtUtc = Status == OnboardingAssignmentStatus.Completed ? now.ToUniversalTime() : null;
        Touch(now);
    }

    /// <summary>Replaced by a new journey before the employee started (for example, after joining a project).</summary>
    public void Supersede(DateTimeOffset now)
    {
        if (Status != OnboardingAssignmentStatus.NotStarted)
            throw new DomainException("Only an onboarding journey that has not started can be replaced.");
        Status = OnboardingAssignmentStatus.Superseded;
        Touch(now);
    }

    public void Cancel(string reason, DateTimeOffset now)
    {
        if (Status == OnboardingAssignmentStatus.Completed) throw new DomainException("A completed onboarding journey cannot be cancelled.");
        if (Status == OnboardingAssignmentStatus.Cancelled) return;
        CancellationReason = Guard.Required(reason, nameof(reason), 500);
        Status = OnboardingAssignmentStatus.Cancelled;
        Touch(now);
    }
}
