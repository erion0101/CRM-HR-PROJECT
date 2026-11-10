namespace HrManagement.Domain;

public sealed class ContractVersion : Entity
{
    private ContractVersion() { }
    public Guid ContractId { get; private set; }
    public int Revision { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string? Notes { get; private set; }

    public static ContractVersion Create(Guid organizationId, Guid contractId, int revision, DateOnly start, DateOnly? end, string? notes)
    {
        Guard.DateRange(start, end, "Contract version");
        return new ContractVersion { OrganizationId = organizationId, ContractId = contractId, Revision = revision, StartDate = start, EndDate = end, Notes = notes?.Trim() };
    }
}
