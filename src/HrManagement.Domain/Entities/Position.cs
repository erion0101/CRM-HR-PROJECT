namespace HrManagement.Domain;

public sealed class Position : SoftDeletableEntity
{
    private Position() { }
    public string Title { get; private set; } = string.Empty;
    public string? Code { get; private set; }
    public Guid? DepartmentId { get; private set; }

    public static Position Create(Guid organizationId, Guid departmentId, string title, string? code) => new()
    {
        OrganizationId = organizationId,
        DepartmentId = Guard.NotEmpty(departmentId, nameof(departmentId)),
        Title = Guard.Required(title, nameof(title), 150),
        Code = string.IsNullOrWhiteSpace(code) ? null : Guard.Required(code, nameof(code), 30).ToUpperInvariant()
    };

    public void Update(Guid departmentId, string title, string? code, DateTimeOffset now)
    {
        DepartmentId = Guard.NotEmpty(departmentId, nameof(departmentId));
        Title = Guard.Required(title, nameof(title), 150);
        Code = string.IsNullOrWhiteSpace(code) ? null : Guard.Required(code, nameof(code), 30).ToUpperInvariant();
        Touch(now);
    }
}
