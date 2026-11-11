namespace HrManagement.Domain;

/// <summary>A manual correction of one person's allowance for one year. Never edited, so the balance keeps its history.</summary>
public sealed class LeaveBalanceAdjustment : Entity
{
    private LeaveBalanceAdjustment() { }
    public Guid EmployeeId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public int Year { get; private set; }
    public decimal Days { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid? CreatedByUserId { get; private set; }

    public static LeaveBalanceAdjustment Create(Guid organizationId, Guid employeeId, Guid leaveTypeId, int year, decimal days,
        string reason, Guid? createdByUserId)
    {
        if (days == 0 || Math.Abs(days) > 366) throw new DomainException("An adjustment must add or remove between 0 and 366 days.");
        if (year is < 2000 or > 2100) throw new DomainException("The adjustment year is not valid.");
        return new LeaveBalanceAdjustment
        {
            OrganizationId = organizationId,
            EmployeeId = Guard.NotEmpty(employeeId, nameof(employeeId)),
            LeaveTypeId = Guard.NotEmpty(leaveTypeId, nameof(leaveTypeId)),
            Year = year,
            Days = decimal.Round(days, 2),
            Reason = Guard.Required(reason, nameof(reason), 500),
            CreatedByUserId = createdByUserId
        };
    }
}
