namespace HrManagement.Domain;

public sealed class PositionAssignment : SoftDeletableEntity
{
    private PositionAssignment() { }
    public Guid EmployeeId { get; private set; }
    public Guid PositionId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    public static PositionAssignment Create(Guid organizationId, Guid employeeId, Guid positionId, Guid departmentId, DateOnly from, DateOnly? to)
    {
        Guard.DateRange(from, to, "Position assignment");
        return new PositionAssignment { OrganizationId = organizationId, EmployeeId = employeeId, PositionId = positionId, DepartmentId = departmentId, EffectiveFrom = from, EffectiveTo = to };
    }

    public void End(DateOnly effectiveTo, DateTimeOffset now)
    {
        Guard.DateRange(EffectiveFrom, effectiveTo, "Position assignment");
        if (EffectiveTo.HasValue && EffectiveTo <= effectiveTo) return;
        EffectiveTo = effectiveTo;
        Touch(now);
    }

    public void Correct(Guid positionId, Guid departmentId, DateTimeOffset now)
    {
        PositionId = Guard.NotEmpty(positionId, nameof(positionId));
        DepartmentId = Guard.NotEmpty(departmentId, nameof(departmentId));
        Touch(now);
    }
}
