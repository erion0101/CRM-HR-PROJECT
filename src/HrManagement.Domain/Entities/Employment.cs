namespace HrManagement.Domain;

public sealed class Employment : SoftDeletableEntity
{
    private Employment() { }
    public Guid EmployeeId { get; private set; }
    public EmploymentStatus Status { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    public static Employment Create(Guid organizationId, Guid employeeId, EmploymentStatus status, DateOnly startDate, DateOnly? endDate)
    {
        Guard.DateRange(startDate, endDate, "Employment");
        return new Employment { OrganizationId = organizationId, EmployeeId = employeeId, Status = status, StartDate = startDate, EndDate = endDate };
    }

    public void Change(EmploymentStatus status, DateOnly startDate, DateOnly? endDate, DateTimeOffset now)
    {
        Guard.DateRange(startDate, endDate, "Employment");
        Status = status;
        StartDate = startDate;
        EndDate = endDate;
        Touch(now);
    }
}
