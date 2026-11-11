using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Leave;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Repositories;

internal sealed class LeaveRepository(AppDbContext db, IAuthorizedDataScope scope) : ILeaveRepository
{
    private Guid OrganizationId => scope.OrganizationId;

    public async Task<IReadOnlyList<LeaveType>> ListTypesAsync(bool includeArchived, CancellationToken cancellationToken) =>
        await db.LeaveTypes.Where(x => x.OrganizationId == OrganizationId && (includeArchived || !x.IsDeleted)).ToListAsync(cancellationToken);

    public Task<LeaveType?> GetTypeAsync(Guid id, CancellationToken cancellationToken) =>
        db.LeaveTypes.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.Id == id, cancellationToken);

    public Task<bool> TypeNameExistsAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        var lowered = name.ToLower();
        return db.LeaveTypes.AnyAsync(x => x.OrganizationId == OrganizationId && !x.IsDeleted && x.Name.ToLower() == lowered &&
            (excludingId == null || x.Id != excludingId), cancellationToken);
    }

    public void AddType(LeaveType type) => db.LeaveTypes.Add(type);

    public async Task<IReadOnlyList<PublicHoliday>> ListHolidaysAsync(CancellationToken cancellationToken) =>
        await db.PublicHolidays.Where(x => x.OrganizationId == OrganizationId).ToListAsync(cancellationToken);

    public Task<PublicHoliday?> GetHolidayAsync(Guid id, CancellationToken cancellationToken) =>
        db.PublicHolidays.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.Id == id, cancellationToken);

    public void AddHoliday(PublicHoliday holiday) => db.PublicHolidays.Add(holiday);
    public void RemoveHoliday(PublicHoliday holiday) => db.PublicHolidays.Remove(holiday);

    public void AddRequest(LeaveRequest request) => db.LeaveRequests.Add(request);

    public Task<LeaveRequest?> GetRequestAsync(Guid id, CancellationToken cancellationToken) =>
        db.LeaveRequests.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<LeaveRequest>> ListRequestsForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken) =>
        await db.LeaveRequests.Where(x => x.OrganizationId == OrganizationId && x.EmployeeId == employeeId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LeaveRequest>> ListPendingAsync(IReadOnlyCollection<Guid>? employeeIds, CancellationToken cancellationToken) =>
        await db.LeaveRequests.Where(x => x.OrganizationId == OrganizationId && x.Status == LeaveRequestStatus.Pending &&
            (employeeIds == null || employeeIds.Contains(x.EmployeeId))).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LeaveRequest>> ListActiveOverlappingAsync(IReadOnlyCollection<Guid> employeeIds, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        await db.LeaveRequests.AsNoTracking().Where(x => x.OrganizationId == OrganizationId && employeeIds.Contains(x.EmployeeId) &&
            (x.Status == LeaveRequestStatus.Pending || x.Status == LeaveRequestStatus.Approved) &&
            x.StartDate <= to && x.EndDate >= from).ToListAsync(cancellationToken);

    /// <summary>Draft employments have not started for real, so they earn no leave.</summary>
    public async Task<IReadOnlyList<EmploymentPeriod>> GetEmploymentPeriodsAsync(Guid employeeId, CancellationToken cancellationToken) =>
        await db.Employments.Where(x => x.OrganizationId == OrganizationId && x.EmployeeId == employeeId && x.Status != EmploymentStatus.Draft)
            .OrderBy(x => x.StartDate).Select(x => new EmploymentPeriod(x.StartDate, x.EndDate)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LeaveBalanceAdjustment>> ListAdjustmentsAsync(Guid employeeId, CancellationToken cancellationToken) =>
        await db.LeaveBalanceAdjustments.Where(x => x.OrganizationId == OrganizationId && x.EmployeeId == employeeId).ToListAsync(cancellationToken);

    public void AddAdjustment(LeaveBalanceAdjustment adjustment) => db.LeaveBalanceAdjustments.Add(adjustment);

    public Task<Guid?> GetCurrentManagerAsync(Guid employeeId, DateOnly today, CancellationToken cancellationToken) =>
        db.ReportingLines.Where(x => x.OrganizationId == OrganizationId && x.EmployeeId == employeeId &&
                x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today))
            .OrderByDescending(x => x.EffectiveFrom).Select(x => (Guid?)x.ManagerEmployeeId).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetDirectReportsAsync(Guid managerEmployeeId, DateOnly today, CancellationToken cancellationToken) =>
        await db.ReportingLines.Where(x => x.OrganizationId == OrganizationId && x.ManagerEmployeeId == managerEmployeeId &&
                x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today))
            .Select(x => x.EmployeeId).Distinct().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetDepartmentEmployeesAsync(Guid departmentId, DateOnly today, CancellationToken cancellationToken)
    {
        var hierarchy = await db.Departments.Where(x => x.OrganizationId == OrganizationId)
            .Select(x => new { x.Id, x.ParentDepartmentId }).ToListAsync(cancellationToken);
        var ids = new HashSet<Guid> { departmentId };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var unit in hierarchy.Where(x => x.ParentDepartmentId is { } parent && ids.Contains(parent)))
                changed |= ids.Add(unit.Id);
        }
        return await db.PositionAssignments.Where(x => x.OrganizationId == OrganizationId && ids.Contains(x.DepartmentId) &&
                x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today))
            .Select(x => x.EmployeeId).Distinct().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetEmployeeNamesAsync(IEnumerable<Guid> employeeIds, CancellationToken cancellationToken)
    {
        var ids = employeeIds.Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<Guid, string>();
        return await db.Employees.Where(x => x.OrganizationId == OrganizationId && ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FirstName + " " + x.LastName, cancellationToken);
    }
}
