namespace HrManagement.Domain;

public sealed class ProjectMembership : Entity
{
    private ProjectMembership() { }

    public Guid ProjectId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string ProjectRole { get; private set; } = string.Empty;
    public string Responsibilities { get; private set; } = string.Empty;
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    public static ProjectMembership Create(Guid organizationId, Guid projectId, Guid employeeId, string projectRole,
        string responsibilities, DateOnly effectiveFrom, DateOnly? effectiveTo)
    {
        Guard.DateRange(effectiveFrom, effectiveTo, "Project membership");
        return new ProjectMembership
        {
            OrganizationId = organizationId,
            ProjectId = projectId,
            EmployeeId = employeeId,
            ProjectRole = Guard.Required(projectRole, nameof(projectRole), 120),
            Responsibilities = Guard.Required(responsibilities, nameof(responsibilities), 1_000),
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo
        };
    }

    public void End(DateOnly effectiveTo, DateTimeOffset now)
    {
        Guard.DateRange(EffectiveFrom, effectiveTo, "Project membership");
        if (EffectiveTo.HasValue && EffectiveTo <= effectiveTo) return;
        EffectiveTo = effectiveTo;
        Touch(now);
    }
}
