namespace HrManagement.Domain;

public sealed class Project : SoftDeletableEntity
{
    private Project() { }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Purpose { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid? DepartmentId { get; private set; }
    public Guid LeadEmployeeId { get; private set; }
    public Guid? OnboardingContactEmployeeId { get; private set; }
    public ProjectStatus Status { get; private set; } = ProjectStatus.Planned;

    public static Project Create(Guid organizationId, string code, string name, string purpose, string description,
        Guid? departmentId, Guid leadEmployeeId, Guid? onboardingContactEmployeeId, ProjectStatus status) => new()
    {
        OrganizationId = organizationId,
        Code = Guard.Required(code, nameof(code), 40).ToUpperInvariant(),
        Name = Guard.Required(name, nameof(name), 160),
        Purpose = Guard.Required(purpose, nameof(purpose), 1_000),
        Description = Guard.Required(description, nameof(description), 4_000),
        DepartmentId = departmentId,
        LeadEmployeeId = leadEmployeeId,
        OnboardingContactEmployeeId = onboardingContactEmployeeId,
        Status = status
    };

    public void Update(string name, string purpose, string description, Guid? departmentId, Guid leadEmployeeId,
        Guid? onboardingContactEmployeeId, ProjectStatus status, DateTimeOffset now)
    {
        if (IsDeleted) throw new DomainException("Archived projects cannot be changed.");
        Name = Guard.Required(name, nameof(name), 160);
        Purpose = Guard.Required(purpose, nameof(purpose), 1_000);
        Description = Guard.Required(description, nameof(description), 4_000);
        DepartmentId = departmentId;
        LeadEmployeeId = leadEmployeeId;
        OnboardingContactEmployeeId = onboardingContactEmployeeId;
        Status = status;
        Touch(now);
    }

    public void ArchiveProject(DateTimeOffset now)
    {
        Status = ProjectStatus.Archived;
        Archive(now);
    }
}
