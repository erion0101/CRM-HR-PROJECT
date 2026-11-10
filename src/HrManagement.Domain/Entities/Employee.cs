namespace HrManagement.Domain;

public sealed class Employee : SoftDeletableEntity
{
    private Employee() { }
    public string EmployeeNumber { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string WorkEmail { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string FullName => $"{FirstName} {LastName}";

    public static Employee Create(Guid organizationId, string employeeNumber, string firstName, string lastName, string workEmail, string? phone, DateOnly? dateOfBirth = null) => new()
    {
        OrganizationId = organizationId,
        EmployeeNumber = Guard.Required(employeeNumber, nameof(employeeNumber), 50).ToUpperInvariant(),
        FirstName = Guard.Required(firstName, nameof(firstName), 100),
        LastName = Guard.Required(lastName, nameof(lastName), 100),
        WorkEmail = Guard.Required(workEmail, nameof(workEmail), 254).ToLowerInvariant(),
        Phone = string.IsNullOrWhiteSpace(phone) ? null : Guard.Required(phone, nameof(phone), 50),
        DateOfBirth = ValidateDateOfBirth(dateOfBirth)
    };

    public void Update(string firstName, string lastName, string workEmail, string? phone, DateOnly? dateOfBirth, DateTimeOffset now)
    {
        FirstName = Guard.Required(firstName, nameof(firstName), 100);
        LastName = Guard.Required(lastName, nameof(lastName), 100);
        WorkEmail = Guard.Required(workEmail, nameof(workEmail), 254).ToLowerInvariant();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : Guard.Required(phone, nameof(phone), 50);
        DateOfBirth = ValidateDateOfBirth(dateOfBirth);
        Touch(now);
    }

    private static DateOnly? ValidateDateOfBirth(DateOnly? value)
    {
        if (value > DateOnly.FromDateTime(DateTime.UtcNow)) throw new DomainException("Date of birth cannot be in the future.");
        return value;
    }
}
