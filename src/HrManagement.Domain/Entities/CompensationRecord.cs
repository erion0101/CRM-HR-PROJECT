namespace HrManagement.Domain;

public sealed class CompensationRecord : SoftDeletableEntity
{
    private CompensationRecord() { }
    public Guid EmployeeId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public string Frequency { get; private set; } = "Monthly";
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    public static CompensationRecord Create(Guid organizationId, Guid employeeId, decimal amount, string currency, string frequency, DateOnly from, DateOnly? to)
    {
        if (amount < 0) throw new DomainException("Compensation cannot be negative.");
        Guard.DateRange(from, to, "Compensation");
        return new CompensationRecord
        {
            OrganizationId = organizationId,
            EmployeeId = employeeId,
            Amount = amount,
            Currency = Guard.Required(currency, nameof(currency), 3).ToUpperInvariant(),
            Frequency = Guard.Required(frequency, nameof(frequency), 30),
            EffectiveFrom = from,
            EffectiveTo = to
        };
    }
}
