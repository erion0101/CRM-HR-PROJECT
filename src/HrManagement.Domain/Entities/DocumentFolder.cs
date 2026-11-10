namespace HrManagement.Domain;

public sealed class DocumentFolder : SoftDeletableEntity
{
    private DocumentFolder() { }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystemFolder { get; private set; }
    public DocumentFolderScope Scope { get; private set; }
    public Guid? EmployeeId { get; private set; }
    public string[] AllowedRoles { get; private set; } = [];

    public static DocumentFolder Create(
        Guid organizationId,
        string name,
        string? description,
        DocumentFolderScope scope,
        Guid? employeeId,
        IEnumerable<string> allowedRoles,
        bool isSystemFolder = false)
    {
        if (scope == DocumentFolderScope.Employee && employeeId is null)
            throw new DomainException("An employee folder requires an employee.");
        if (scope == DocumentFolderScope.Global && employeeId is not null)
            throw new DomainException("A global folder cannot be assigned to an employee.");

        var roles = allowedRoles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.Ordinal).ToArray();
        if (roles.Length == 0) throw new DomainException("At least one allowed role is required.");

        return new DocumentFolder
        {
            OrganizationId = organizationId,
            Name = Guard.Required(name, nameof(name), 120),
            Description = string.IsNullOrWhiteSpace(description) ? null : Guard.Required(description, nameof(description), 500),
            IsSystemFolder = isSystemFolder,
            Scope = scope,
            EmployeeId = employeeId,
            AllowedRoles = roles
        };
    }

    public void SetAllowedRoles(IEnumerable<string> allowedRoles, DateTimeOffset changedAtUtc)
    {
        var roles = allowedRoles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.Ordinal).ToArray();
        if (roles.Length == 0) throw new DomainException("At least one allowed role is required.");
        AllowedRoles = roles;
        Touch(changedAtUtc);
    }
}
