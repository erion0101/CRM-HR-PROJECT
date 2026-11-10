namespace HrManagement.Domain;

public sealed class Contract : SoftDeletableEntity
{
    private Contract() { }
    public Guid EmployeeId { get; private set; }
    public string ContractNumber { get; private set; } = string.Empty;
    public ContractStatus Status { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public int CurrentRevision { get; private set; } = 1;

    public static Contract Create(Guid organizationId, Guid employeeId, string number, DateOnly start, DateOnly? end)
    {
        Guard.DateRange(start, end, "Contract");
        return new Contract
        {
            OrganizationId = organizationId,
            EmployeeId = employeeId,
            ContractNumber = Guard.Required(number, nameof(number), 80).ToUpperInvariant(),
            Status = ContractStatus.Draft,
            StartDate = start,
            EndDate = end
        };
    }

    public void AddRevision(DateOnly start, DateOnly? end, DateTimeOffset now)
    {
        Guard.DateRange(start, end, "Contract");
        StartDate = start;
        EndDate = end;
        CurrentRevision++;
        Touch(now);
    }

    public void Activate(DateTimeOffset now) { Status = ContractStatus.Active; Touch(now); }

    public void Terminate(DateOnly date, DateTimeOffset now)
    {
        if (date < StartDate) throw new DomainException("Termination date cannot precede contract start.");
        EndDate = date;
        Status = ContractStatus.Terminated;
        Touch(now);
    }
}
