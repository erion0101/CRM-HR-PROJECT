using HrManagement.Domain;

namespace HrManagement.Application.Features.Leave;

/// <summary>Leave data for the caller's organization. Handlers decide who may see or change which employee.</summary>
public interface ILeaveRepository
{
    Task<IReadOnlyList<LeaveType>> ListTypesAsync(bool includeArchived, CancellationToken cancellationToken);
    Task<LeaveType?> GetTypeAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> TypeNameExistsAsync(string name, Guid? excludingId, CancellationToken cancellationToken);
    void AddType(LeaveType type);

    Task<IReadOnlyList<PublicHoliday>> ListHolidaysAsync(CancellationToken cancellationToken);
    Task<PublicHoliday?> GetHolidayAsync(Guid id, CancellationToken cancellationToken);
    void AddHoliday(PublicHoliday holiday);
    void RemoveHoliday(PublicHoliday holiday);

    void AddRequest(LeaveRequest request);
    Task<LeaveRequest?> GetRequestAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeaveRequest>> ListRequestsForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken);
    /// <summary>Pending requests, for the given employees only, or for everyone when <paramref name="employeeIds"/> is null.</summary>
    Task<IReadOnlyList<LeaveRequest>> ListPendingAsync(IReadOnlyCollection<Guid>? employeeIds, CancellationToken cancellationToken);
    /// <summary>Pending and approved requests that touch the period.</summary>
    Task<IReadOnlyList<LeaveRequest>> ListActiveOverlappingAsync(IReadOnlyCollection<Guid> employeeIds, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    Task<IReadOnlyList<EmploymentPeriod>> GetEmploymentPeriodsAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeaveBalanceAdjustment>> ListAdjustmentsAsync(Guid employeeId, CancellationToken cancellationToken);
    void AddAdjustment(LeaveBalanceAdjustment adjustment);

    Task<Guid?> GetCurrentManagerAsync(Guid employeeId, DateOnly today, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetDirectReportsAsync(Guid managerEmployeeId, DateOnly today, CancellationToken cancellationToken);
    /// <summary>Employees in the department (and its sub-departments) on <paramref name="today"/>.</summary>
    Task<IReadOnlyList<Guid>> GetDepartmentEmployeesAsync(Guid departmentId, DateOnly today, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, string>> GetEmployeeNamesAsync(IEnumerable<Guid> employeeIds, CancellationToken cancellationToken);
}
