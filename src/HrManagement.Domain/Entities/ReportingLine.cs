namespace HrManagement.Domain;

public sealed class ReportingLine : SoftDeletableEntity
{
    private ReportingLine() { }
    public Guid EmployeeId { get; private set; }
    public Guid ManagerEmployeeId { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    public static ReportingLine Create(Guid organizationId, Guid employeeId, Guid managerEmployeeId, DateOnly from, DateOnly? to)
    {
        if (employeeId == managerEmployeeId) throw new DomainException("An employee cannot manage themselves.");
        Guard.DateRange(from, to, "Reporting line");
        return new ReportingLine { OrganizationId = organizationId, EmployeeId = employeeId, ManagerEmployeeId = managerEmployeeId, EffectiveFrom = from, EffectiveTo = to };
    }

    public void End(DateOnly effectiveTo, DateTimeOffset now)
    {
        Guard.DateRange(EffectiveFrom, effectiveTo, "Reporting line");
        if (EffectiveTo.HasValue && EffectiveTo <= effectiveTo) return;
        EffectiveTo = effectiveTo;
        Touch(now);
    }

    public void Correct(Guid managerEmployeeId, DateTimeOffset now)
    {
        if (EmployeeId == managerEmployeeId) throw new DomainException("An employee cannot manage themselves.");
        ManagerEmployeeId = Guard.NotEmpty(managerEmployeeId, nameof(managerEmployeeId));
        Touch(now);
    }
}
