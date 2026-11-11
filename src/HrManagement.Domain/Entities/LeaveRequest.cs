namespace HrManagement.Domain;

/// <summary>A request for whole days off. Working days are calculated by the server when the request is made.</summary>
public sealed class LeaveRequest : Entity
{
    private LeaveRequest() { }
    public Guid EmployeeId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public int WorkingDays { get; private set; }
    public string? Reason { get; private set; }
    public LeaveRequestStatus Status { get; private set; }
    /// <summary>The employee's manager when the request was made; null when they have none (HR decides).</summary>
    public Guid? ApproverEmployeeId { get; private set; }
    public Guid? DecidedByUserId { get; private set; }
    public string? DecisionComment { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }

    public bool Covers(DateOnly day) => StartDate <= day && day <= EndDate;
    public bool IsActive => Status is LeaveRequestStatus.Pending or LeaveRequestStatus.Approved;

    public static LeaveRequest Create(Guid organizationId, Guid employeeId, LeaveType type, DateOnly startDate, DateOnly endDate,
        int workingDays, string? reason, Guid? approverEmployeeId, DateTimeOffset now)
    {
        if (type.IsDeleted) throw new DomainException("This leave type is no longer available.");
        Guard.DateRange(startDate, endDate, "Leave");
        if (startDate.Year != endDate.Year)
            throw new DomainException("Time off cannot cross into a new year. Make one request for each year.");
        if (endDate.DayNumber - startDate.DayNumber > 92) throw new DomainException("A single request cannot be longer than three months.");
        if (workingDays <= 0) throw new DomainException("The selected dates contain no working days.");
        if (approverEmployeeId == employeeId) approverEmployeeId = null;
        var request = new LeaveRequest
        {
            OrganizationId = organizationId,
            EmployeeId = Guard.NotEmpty(employeeId, nameof(employeeId)),
            LeaveTypeId = type.Id,
            StartDate = startDate,
            EndDate = endDate,
            WorkingDays = workingDays,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : Guard.Required(reason, nameof(reason), 500),
            Status = LeaveRequestStatus.Pending,
            ApproverEmployeeId = approverEmployeeId
        };
        if (!type.RequiresApproval)
        {
            request.Status = LeaveRequestStatus.Approved;
            request.DecidedAtUtc = now.ToUniversalTime();
        }
        return request;
    }

    public void Approve(Guid decidedByUserId, string? comment, DateTimeOffset now) =>
        Decide(LeaveRequestStatus.Approved, decidedByUserId, comment, now);

    public void Reject(Guid decidedByUserId, string comment, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(comment)) throw new DomainException("Say why the request is rejected.");
        Decide(LeaveRequestStatus.Rejected, decidedByUserId, comment, now);
    }

    /// <summary>The employee can cancel a pending request, or an approved one that has not started yet.</summary>
    public void Cancel(DateOnly today, DateTimeOffset now)
    {
        if (Status == LeaveRequestStatus.Approved && StartDate <= today)
            throw new DomainException("Time off that has already started cannot be cancelled. Ask HR to correct it.");
        if (!IsActive) throw new DomainException("Only pending or approved requests can be cancelled.");
        Status = LeaveRequestStatus.Cancelled;
        Touch(now);
    }

    private void Decide(LeaveRequestStatus status, Guid decidedByUserId, string? comment, DateTimeOffset now)
    {
        if (Status != LeaveRequestStatus.Pending) throw new DomainException("This request has already been decided.");
        Status = status;
        DecidedByUserId = Guard.NotEmpty(decidedByUserId, nameof(decidedByUserId));
        DecisionComment = string.IsNullOrWhiteSpace(comment) ? null : Guard.Required(comment, nameof(comment), 500);
        DecidedAtUtc = now.ToUniversalTime();
        Touch(now);
    }
}
