using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Models;
using HrManagement.Application.Features.Dashboard;
using HrManagement.Application.Features.Employees;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeRepository(AppDbContext db, IAuthorizedDataScope dataScope) : IEmployeeRepository
{
    public async Task<PageResult<EmployeeDetailsData>> SearchAccessibleAsync(string? search, Guid? departmentId, EmploymentStatus? status, string? cursor, int limit, CancellationToken cancellationToken)
    {
        var allowed = await dataScope.GetAccessibleEmployeeIdsAsync(cancellationToken);
        var query = db.Employees.Where(x => allowed.Contains(x.Id));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.FirstName.ToLower().Contains(term) || x.LastName.ToLower().Contains(term) || x.EmployeeNumber.ToLower().Contains(term) || x.WorkEmail.ToLower().Contains(term));
        }
        if (departmentId is not null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            query = query.Where(x => db.PositionAssignments.Any(a => a.EmployeeId == x.Id && a.DepartmentId == departmentId && a.EffectiveFrom <= today && (a.EffectiveTo == null || a.EffectiveTo >= today)));
        }
        if (status is not null) query = query.Where(x => db.Employments.Any(e => e.EmployeeId == x.Id && e.Status == status));
        query = query.OrderBy(x => x.Id);
        if (Guid.TryParse(cursor, out var after)) query = query.Where(x => x.Id.CompareTo(after) > 0).OrderBy(x => x.Id);
        var employees = await query.Take(limit + 1).ToListAsync(cancellationToken);
        var hasMore = employees.Count > limit;
        if (hasMore) employees.RemoveAt(employees.Count - 1);
        var details = new List<EmployeeDetailsData>(employees.Count);
        foreach (var employee in employees) details.Add(await GetDetailsAsync(employee, cancellationToken));
        return new PageResult<EmployeeDetailsData>(details, hasMore && employees.Count > 0 ? employees[^1].Id.ToString() : null);
    }

    public async Task<Employee?> GetAccessibleAsync(Guid id, CancellationToken cancellationToken)
    {
        var allowed = await dataScope.GetAccessibleEmployeeIdsAsync(cancellationToken);
        return await db.Employees.SingleOrDefaultAsync(x => x.Id == id && allowed.Contains(x.Id), cancellationToken);
    }

    public async Task<bool> ExistsAccessibleAsync(Guid id, CancellationToken cancellationToken) =>
        await dataScope.CanAccessEmployeeAsync(id, cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetAccessibleIdsAsync(CancellationToken cancellationToken) =>
        await dataScope.GetAccessibleEmployeeIdsAsync(cancellationToken);

    public async Task<EmployeeDetailsData> GetDetailsAsync(Employee employee, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var employment = await db.Employments.Where(x => x.EmployeeId == employee.Id && x.StartDate <= today && (x.EndDate == null || x.EndDate >= today)).OrderByDescending(x => x.StartDate).FirstOrDefaultAsync(cancellationToken);
        var assignment = await db.PositionAssignments.Where(x => x.EmployeeId == employee.Id && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(cancellationToken);
        var reporting = await db.ReportingLines.Where(x => x.EmployeeId == employee.Id && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(cancellationToken);
        return new EmployeeDetailsData(employee, employment, assignment, reporting);
    }

    public async Task<EmployeeProfileDto?> GetProfileAccessibleAsync(Guid id, CancellationToken cancellationToken)
    {
        var allowed = await dataScope.GetAccessibleEmployeeIdsAsync(cancellationToken);
        var employee = await db.Employees.SingleOrDefaultAsync(x => x.Id == id && allowed.Contains(x.Id), cancellationToken);
        if (employee is null) return null;

        var details = await GetDetailsAsync(employee, cancellationToken);
        var organizationPath = details.Assignment is null
            ? []
            : await GetOrganizationPathAsync(details.Assignment.DepartmentId, cancellationToken);
        var department = organizationPath.LastOrDefault();
        var position = details.Assignment is null
            ? null
            : await db.Positions.Where(x => x.Id == details.Assignment.PositionId)
                .Select(x => new OrganizationReferenceDto(x.Id, x.Title, x.Code)).SingleOrDefaultAsync(cancellationToken);
        var manager = details.ReportingLine is null
            ? null
            : await db.Employees.Where(x => x.Id == details.ReportingLine.ManagerEmployeeId)
                .Select(x => new EmployeeReferenceDto(x.Id, x.EmployeeNumber, x.FirstName + " " + x.LastName, x.WorkEmail))
                .SingleOrDefaultAsync(cancellationToken);
        var hireDate = await db.Employments.Where(x => x.EmployeeId == employee.Id).OrderBy(x => x.StartDate)
            .Select(x => (DateOnly?)x.StartDate).FirstOrDefaultAsync(cancellationToken);
        var directReports = await GetDirectReportsAsync(employee.Id, cancellationToken);

        return new EmployeeProfileDto(
            employee.Id,
            employee.EmployeeNumber,
            employee.FirstName,
            employee.LastName,
            employee.WorkEmail,
            employee.Phone,
            employee.DateOfBirth,
            details.Employment?.Status,
            hireDate,
            details.Employment?.EndDate,
            department,
            organizationPath,
            position,
            manager,
            directReports,
            employee.Version);
    }

    private async Task<IReadOnlyList<OrganizationReferenceDto>> GetOrganizationPathAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        var path = new List<OrganizationReferenceDto>();
        var visited = new HashSet<Guid>();
        Guid? currentId = departmentId;
        while (currentId is { } id && visited.Add(id))
        {
            var item = await db.Departments.Where(x => x.Id == id)
                .Select(x => new { Reference = new OrganizationReferenceDto(x.Id, x.Name, x.Code), x.ParentDepartmentId })
                .SingleOrDefaultAsync(cancellationToken);
            if (item is null) break;
            path.Add(item.Reference);
            currentId = item.ParentDepartmentId;
        }
        path.Reverse();
        return path;
    }

    public async Task<IReadOnlyList<EmploymentHistoryDto>> GetEmploymentHistoryAsync(Guid employeeId, CancellationToken cancellationToken) =>
        await db.Employments.Where(x => x.EmployeeId == employeeId).OrderByDescending(x => x.StartDate)
            .Select(x => new EmploymentHistoryDto(x.Id, x.Status, x.StartDate, x.EndDate, x.Version))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PositionAssignmentHistoryDto>> GetAssignmentHistoryAsync(Guid employeeId, CancellationToken cancellationToken) =>
        await (from assignment in db.PositionAssignments
               join position in db.Positions on assignment.PositionId equals position.Id
               join department in db.Departments on assignment.DepartmentId equals department.Id
               where assignment.EmployeeId == employeeId
               orderby assignment.EffectiveFrom descending
               select new PositionAssignmentHistoryDto(
                   assignment.Id,
                   new OrganizationReferenceDto(position.Id, position.Title, position.Code),
                   new OrganizationReferenceDto(department.Id, department.Name, department.Code),
                   assignment.EffectiveFrom,
                   assignment.EffectiveTo,
                   assignment.Version)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReportingLineHistoryDto>> GetReportingLineHistoryAsync(Guid employeeId, CancellationToken cancellationToken) =>
        await (from reportingLine in db.ReportingLines
               join manager in db.Employees on reportingLine.ManagerEmployeeId equals manager.Id
               where reportingLine.EmployeeId == employeeId
               orderby reportingLine.EffectiveFrom descending
               select new ReportingLineHistoryDto(
                   reportingLine.Id,
                   new EmployeeReferenceDto(manager.Id, manager.EmployeeNumber, manager.FirstName + " " + manager.LastName, manager.WorkEmail),
                   reportingLine.EffectiveFrom,
                   reportingLine.EffectiveTo,
                   reportingLine.Version)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EmployeeReferenceDto>> GetDirectReportsAsync(Guid managerEmployeeId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var allowed = await dataScope.GetAccessibleEmployeeIdsAsync(cancellationToken);
        return await (from reportingLine in db.ReportingLines
                      join employee in db.Employees on reportingLine.EmployeeId equals employee.Id
                      where reportingLine.ManagerEmployeeId == managerEmployeeId &&
                            reportingLine.EffectiveFrom <= today &&
                            (reportingLine.EffectiveTo == null || reportingLine.EffectiveTo >= today) &&
                            allowed.Contains(employee.Id)
                      orderby employee.LastName, employee.FirstName
                      select new EmployeeReferenceDto(employee.Id, employee.EmployeeNumber, employee.FirstName + " " + employee.LastName, employee.WorkEmail))
            .ToListAsync(cancellationToken);
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var allowed = await dataScope.GetAccessibleEmployeeIdsAsync(cancellationToken);
        var visibleEmployees = db.Employees.Where(x => allowed.Contains(x.Id));
        var currentEmployments = db.Employments.Where(x => allowed.Contains(x.EmployeeId) && x.StartDate <= today && (x.EndDate == null || x.EndDate >= today));
        var currentAssignments = db.PositionAssignments.Where(x => allowed.Contains(x.EmployeeId) && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today));

        var total = await visibleEmployees.CountAsync(cancellationToken);
        var active = await currentEmployments.CountAsync(x => x.Status == EmploymentStatus.Active, cancellationToken);
        // On leave today: an employment marked On leave, or approved time off that covers today.
        var approvedLeaveToday = db.LeaveRequests.Where(x => allowed.Contains(x.EmployeeId) && x.Status == LeaveRequestStatus.Approved &&
            x.StartDate <= today && x.EndDate >= today);
        var onLeaveEmployees = visibleEmployees.Where(employee =>
            currentEmployments.Any(employment => employment.EmployeeId == employee.Id && employment.Status == EmploymentStatus.OnLeave) ||
            approvedLeaveToday.Any(leave => leave.EmployeeId == employee.Id));
        var onLeave = await onLeaveEmployees.CountAsync(cancellationToken);
        var configured = await currentEmployments.Select(x => x.EmployeeId).Distinct().CountAsync(cancellationToken);
        var organizationUnits = await currentAssignments.Select(x => x.DepartmentId).Distinct().CountAsync(cancellationToken);
        var recentEntities = await visibleEmployees.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).Take(6).ToListAsync(cancellationToken);
        var recent = new List<DashboardEmployeeDto>(recentEntities.Count);
        foreach (var employee in recentEntities)
            recent.Add(await ToDashboardEmployeeAsync(employee, cancellationToken));

        var peopleOnLeaveEntities = await onLeaveEmployees
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName).Take(6).ToListAsync(cancellationToken);
        var peopleOnLeave = new List<DashboardEmployeeDto>(peopleOnLeaveEntities.Count);
        foreach (var employee in peopleOnLeaveEntities)
            peopleOnLeave.Add(await ToDashboardEmployeeAsync(employee, cancellationToken));

        var celebrationEmployees = await visibleEmployees
            .Select(employee => new { employee.Id, employee.FirstName, employee.LastName, employee.DateOfBirth })
            .ToListAsync(cancellationToken);
        var hireDates = await db.Employments.Where(x => allowed.Contains(x.EmployeeId))
            .GroupBy(x => x.EmployeeId)
            .Select(group => new { EmployeeId = group.Key, HireDate = group.Min(x => x.StartDate) })
            .ToDictionaryAsync(x => x.EmployeeId, x => x.HireDate, cancellationToken);
        var celebrationWindowEnd = today.AddDays(90);
        var celebrations = new List<DashboardCelebrationDto>();
        foreach (var employee in celebrationEmployees)
        {
            if (employee.DateOfBirth is { } birthDate)
            {
                var birthday = NextOccurrence(birthDate.Month, birthDate.Day, today);
                if (birthday <= celebrationWindowEnd)
                    celebrations.Add(new DashboardCelebrationDto(employee.Id, $"{employee.FirstName} {employee.LastName}", "Birthday", birthday, null));
            }

            if (hireDates.TryGetValue(employee.Id, out var hireDate))
            {
                var anniversary = NextOccurrence(hireDate.Month, hireDate.Day, today);
                var years = anniversary.Year - hireDate.Year;
                if (years > 0 && anniversary <= celebrationWindowEnd)
                    celebrations.Add(new DashboardCelebrationDto(employee.Id, $"{employee.FirstName} {employee.LastName}", "WorkAnniversary", anniversary, years));
            }
        }

        var upcomingCelebrations = celebrations.OrderBy(x => x.Date).ThenBy(x => x.FullName).Take(6).ToArray();
        return new DashboardSummaryDto(total, active, onLeave, total - configured, organizationUnits, recent, upcomingCelebrations, peopleOnLeave);
    }

    private async Task<DashboardEmployeeDto> ToDashboardEmployeeAsync(Employee employee, CancellationToken cancellationToken)
    {
        var details = await GetDetailsAsync(employee, cancellationToken);
        var position = details.Assignment is null
            ? null
            : await db.Positions.Where(x => x.Id == details.Assignment.PositionId).Select(x => x.Title).SingleOrDefaultAsync(cancellationToken);
        var department = details.Assignment is null
            ? null
            : await db.Departments.Where(x => x.Id == details.Assignment.DepartmentId).Select(x => x.Name).SingleOrDefaultAsync(cancellationToken);
        var hireDate = await db.Employments.Where(x => x.EmployeeId == employee.Id).OrderBy(x => x.StartDate)
            .Select(x => (DateOnly?)x.StartDate).FirstOrDefaultAsync(cancellationToken);
        return new DashboardEmployeeDto(employee.Id, employee.EmployeeNumber, employee.FullName, employee.WorkEmail, details.Employment?.Status, position, department, hireDate);
    }

    private static DateOnly NextOccurrence(int month, int day, DateOnly from)
    {
        var safeDay = Math.Min(day, DateTime.DaysInMonth(from.Year, month));
        var occurrence = new DateOnly(from.Year, month, safeDay);
        if (occurrence >= from) return occurrence;
        var nextYear = from.Year + 1;
        return new DateOnly(nextYear, month, Math.Min(day, DateTime.DaysInMonth(nextYear, month)));
    }

    public void Add(Employee employee) => db.Employees.Add(employee);
    public Task<Employment?> GetOpenEmploymentAsync(Guid employeeId, CancellationToken cancellationToken) => db.Employments.SingleOrDefaultAsync(x => x.EmployeeId == employeeId && x.EndDate == null, cancellationToken);
    public void AddEmployment(Employment employment) => db.Employments.Add(employment);
    public Task<bool> HasOverlappingAssignmentAsync(Guid employeeId, DateOnly from, DateOnly? to, Guid? excludingAssignmentId, CancellationToken cancellationToken) =>
        db.PositionAssignments.AnyAsync(x => x.EmployeeId == employeeId && (!excludingAssignmentId.HasValue || x.Id != excludingAssignmentId) &&
            x.EffectiveFrom <= (to ?? DateOnly.MaxValue) && from <= (x.EffectiveTo ?? DateOnly.MaxValue), cancellationToken);
    public Task<PositionAssignment?> GetAssignmentOnDateAsync(Guid employeeId, DateOnly onDate, CancellationToken cancellationToken) =>
        db.PositionAssignments.Where(x => x.EmployeeId == employeeId && x.EffectiveFrom <= onDate && (x.EffectiveTo == null || x.EffectiveTo >= onDate))
            .OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(cancellationToken);
    public void AddAssignment(PositionAssignment assignment) => db.PositionAssignments.Add(assignment);
    public Task<ReportingLine?> GetReportingLineOnDateAsync(Guid employeeId, DateOnly onDate, CancellationToken cancellationToken) =>
        db.ReportingLines.Where(x => x.EmployeeId == employeeId && x.EffectiveFrom <= onDate && (x.EffectiveTo == null || x.EffectiveTo >= onDate))
            .OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(cancellationToken);
    public Task<bool> HasOverlappingReportingLineAsync(Guid employeeId, DateOnly from, DateOnly? to, Guid? excludingReportingLineId, CancellationToken cancellationToken) =>
        db.ReportingLines.AnyAsync(x => x.EmployeeId == employeeId && (!excludingReportingLineId.HasValue || x.Id != excludingReportingLineId) &&
            x.EffectiveFrom <= (to ?? DateOnly.MaxValue) && from <= (x.EffectiveTo ?? DateOnly.MaxValue), cancellationToken);
    public void AddReportingLine(ReportingLine reportingLine) => db.ReportingLines.Add(reportingLine);

}
