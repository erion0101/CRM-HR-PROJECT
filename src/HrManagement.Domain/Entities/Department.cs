namespace HrManagement.Domain;

public sealed class Department : SoftDeletableEntity
{
    private Department() { }
    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }
    public OrganizationUnitType Type { get; private set; } = OrganizationUnitType.Department;
    public Guid? ParentDepartmentId { get; private set; }
    public Guid? ManagerEmployeeId { get; private set; }
    public Guid? HrEmployeeId { get; private set; }
    public Guid? ProductManagerEmployeeId { get; private set; }
    public string? Description { get; private set; }

    public static Department Create(
        Guid organizationId,
        string name,
        string? code,
        OrganizationUnitType type = OrganizationUnitType.Department,
        Guid? parentDepartmentId = null,
        Guid? managerEmployeeId = null,
        string? description = null,
        Guid? hrEmployeeId = null,
        Guid? productManagerEmployeeId = null)
    {
        EnsureDistinctLeadership(managerEmployeeId, hrEmployeeId, productManagerEmployeeId);
        var department = new Department
        {
            OrganizationId = organizationId,
            Name = Guard.Required(name, nameof(name), 150),
            Code = string.IsNullOrWhiteSpace(code) ? null : Guard.Required(code, nameof(code), 30).ToUpperInvariant(),
            Type = type,
            ParentDepartmentId = parentDepartmentId,
            ManagerEmployeeId = managerEmployeeId,
            HrEmployeeId = hrEmployeeId,
            ProductManagerEmployeeId = productManagerEmployeeId,
            Description = string.IsNullOrWhiteSpace(description) ? null : Guard.Required(description, nameof(description), 500)
        };
        department.EnsureNotItsOwnParent(parentDepartmentId);
        return department;
    }

    public void Update(string name, string? code, OrganizationUnitType type, Guid? parentDepartmentId, Guid? managerEmployeeId,
        Guid? hrEmployeeId, Guid? productManagerEmployeeId, string? description, DateTimeOffset now)
    {
        EnsureNotItsOwnParent(parentDepartmentId);
        EnsureDistinctLeadership(managerEmployeeId, hrEmployeeId, productManagerEmployeeId);
        Name = Guard.Required(name, nameof(name), 150);
        Code = string.IsNullOrWhiteSpace(code) ? null : Guard.Required(code, nameof(code), 30).ToUpperInvariant();
        Type = type;
        ParentDepartmentId = parentDepartmentId;
        ManagerEmployeeId = managerEmployeeId;
        HrEmployeeId = hrEmployeeId;
        ProductManagerEmployeeId = productManagerEmployeeId;
        Description = string.IsNullOrWhiteSpace(description) ? null : Guard.Required(description, nameof(description), 500);
        Touch(now);
    }

    public void Update(string name, string? code, OrganizationUnitType type, Guid? parentDepartmentId, Guid? managerEmployeeId,
        string? description, DateTimeOffset now) =>
        Update(name, code, type, parentDepartmentId, managerEmployeeId, HrEmployeeId, ProductManagerEmployeeId, description, now);

    public void Update(string name, string? code, OrganizationUnitType type, Guid? parentDepartmentId, DateTimeOffset now) =>
        Update(name, code, type, parentDepartmentId, ManagerEmployeeId, HrEmployeeId, ProductManagerEmployeeId, Description, now);

    public static bool CanContain(OrganizationUnitType parent, OrganizationUnitType child) =>
        HierarchyRank(parent) < HierarchyRank(child);

    private static int HierarchyRank(OrganizationUnitType type) => type switch
    {
        OrganizationUnitType.BusinessUnit => 0,
        OrganizationUnitType.Division => 1,
        OrganizationUnitType.Department => 2,
        OrganizationUnitType.Team => 3,
        _ => throw new DomainException("Unsupported organization unit type.")
    };

    private void EnsureNotItsOwnParent(Guid? parentDepartmentId)
    {
        if (parentDepartmentId == Id) throw new DomainException("An organization unit cannot be its own parent.");
    }

    private static void EnsureDistinctLeadership(Guid? managerEmployeeId, Guid? hrEmployeeId, Guid? productManagerEmployeeId)
    {
        var assignments = new[] { managerEmployeeId, hrEmployeeId, productManagerEmployeeId }.OfType<Guid>().ToArray();
        if (assignments.Distinct().Count() != assignments.Length)
            throw new DomainException("Manager, HR, and Product Manager must be assigned to different employees.");
    }
}
