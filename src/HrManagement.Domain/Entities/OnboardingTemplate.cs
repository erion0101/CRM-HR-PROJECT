namespace HrManagement.Domain;

public sealed class OnboardingTemplate : Entity
{
    private OnboardingTemplate() { }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public OnboardingTemplateKind Kind { get; private set; } = OnboardingTemplateKind.General;
    public Guid? ProjectId { get; private set; }
    /// <summary>The company package assigned automatically when an employee's employment first becomes active.</summary>
    public bool AutoAssignToNewEmployees { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid? PositionId { get; private set; }
    public int DurationDays { get; private set; }
    public OnboardingTemplateStatus Status { get; private set; } = OnboardingTemplateStatus.Draft;
    public int CurrentVersion { get; private set; } = 1;
    public int? PublishedVersion { get; private set; }

    public static OnboardingTemplate Create(Guid organizationId, string name, string description, Guid? departmentId,
        Guid? positionId, int durationDays, OnboardingTemplateKind kind = OnboardingTemplateKind.General, Guid? projectId = null)
    {
        if (kind == OnboardingTemplateKind.Project && projectId is null)
            throw new DomainException("A project onboarding package must belong to a project.");
        if (kind != OnboardingTemplateKind.Project && projectId is not null)
            throw new DomainException("Only a project onboarding package can belong to a project.");
        return new OnboardingTemplate
        {
            OrganizationId = organizationId,
            Name = Guard.Required(name, nameof(name), 160),
            Description = Guard.Required(description, nameof(description), 1_000),
            Kind = kind,
            ProjectId = projectId,
            DepartmentId = departmentId,
            PositionId = positionId,
            DurationDays = ValidateDuration(durationDays)
        };
    }

    /// <summary>Published and not retired: its published version can be assigned.</summary>
    public bool IsAssignable => PublishedVersion is not null && Status != OnboardingTemplateStatus.Retired;

    public void UpdateDraft(string name, string description, Guid? departmentId, Guid? positionId, int durationDays, DateTimeOffset now)
    {
        if (Status != OnboardingTemplateStatus.Draft) throw new DomainException("Only draft onboarding templates can be updated.");
        Name = Guard.Required(name, nameof(name), 160);
        Description = Guard.Required(description, nameof(description), 1_000);
        DepartmentId = departmentId;
        PositionId = positionId;
        DurationDays = ValidateDuration(durationDays);
        Touch(now);
    }

    public void Publish(DateTimeOffset now)
    {
        if (Status != OnboardingTemplateStatus.Draft) throw new DomainException("Only draft onboarding templates can be published.");
        Status = OnboardingTemplateStatus.Published;
        PublishedVersion = CurrentVersion;
        Touch(now);
    }

    public int BeginNewVersion(DateTimeOffset now)
    {
        if (Status != OnboardingTemplateStatus.Published) throw new DomainException("Only a published template can begin a new version.");
        CurrentVersion++;
        Status = OnboardingTemplateStatus.Draft;
        Touch(now);
        return CurrentVersion;
    }

    public void SetAutoAssign(bool enabled, DateTimeOffset now)
    {
        if (enabled && Kind != OnboardingTemplateKind.Company)
            throw new DomainException("Only a company package can be assigned automatically to new employees.");
        if (enabled && !IsAssignable)
            throw new DomainException("Publish the company package before assigning it automatically.");
        if (AutoAssignToNewEmployees == enabled) return;
        AutoAssignToNewEmployees = enabled;
        Touch(now);
    }

    public void Retire(DateTimeOffset now)
    {
        if (Status == OnboardingTemplateStatus.Retired) return;
        if (PublishedVersion is null) throw new DomainException("An unpublished template cannot be retired.");
        Status = OnboardingTemplateStatus.Retired;
        AutoAssignToNewEmployees = false;
        Touch(now);
    }

    private static int ValidateDuration(int durationDays) => durationDays is >= 1 and <= 365
        ? durationDays
        : throw new DomainException("Onboarding duration must be between 1 and 365 days.");
}
