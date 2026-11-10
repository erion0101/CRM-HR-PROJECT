using HrManagement.Application.Common.Models;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Departments;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Repositories;

internal sealed class DepartmentRepository(AppDbContext db, IAuthorizedDataScope dataScope) : IDepartmentRepository
{
    public async Task<PageResult<DepartmentDetailsData>> ListAsync(Guid organizationId, string? cursor, int limit, CancellationToken cancellationToken)
    {
        var query = db.Departments.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id);
        if (Guid.TryParse(cursor, out var after)) query = query.Where(x => x.Id.CompareTo(after) > 0).OrderBy(x => x.Id);
        var values = await query.Take(limit + 1).ToListAsync(cancellationToken);
        var hasMore = values.Count > limit;
        if (hasMore) values.RemoveAt(values.Count - 1);
        var allUnits = await db.Departments.AsNoTracking().Where(x => x.OrganizationId == organizationId).ToListAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var directCounts = await db.PositionAssignments
            .Where(x => x.OrganizationId == organizationId && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today))
            .GroupBy(x => x.DepartmentId)
            .Select(x => new { DepartmentId = x.Key, Count = x.Select(a => a.EmployeeId).Distinct().Count() })
            .ToDictionaryAsync(x => x.DepartmentId, x => x.Count, cancellationToken);
        var leadershipIds = values.SelectMany(x => new[] { x.ManagerEmployeeId, x.HrEmployeeId, x.ProductManagerEmployeeId })
            .OfType<Guid>().Distinct().ToArray();
        var leaders = await db.Employees.Where(x => leadershipIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FirstName + " " + x.LastName, cancellationToken);
        var children = allUnits.Where(x => x.ParentDepartmentId != null)
            .GroupBy(x => x.ParentDepartmentId!.Value)
            .ToDictionary(x => x.Key, x => x.ToArray());

        int TotalEmployees(Guid unitId, HashSet<Guid> visited)
        {
            if (!visited.Add(unitId)) return 0;
            var total = directCounts.GetValueOrDefault(unitId);
            foreach (var child in children.GetValueOrDefault(unitId) ?? []) total += TotalEmployees(child.Id, visited);
            return total;
        }

        var details = values.Select(unit => new DepartmentDetailsData(
            unit,
            unit.ManagerEmployeeId is { } managerId ? leaders.GetValueOrDefault(managerId) : null,
            unit.HrEmployeeId is { } hrId ? leaders.GetValueOrDefault(hrId) : null,
            unit.ProductManagerEmployeeId is { } productManagerId ? leaders.GetValueOrDefault(productManagerId) : null,
            directCounts.GetValueOrDefault(unit.Id),
            TotalEmployees(unit.Id, []),
            children.GetValueOrDefault(unit.Id)?.Length ?? 0)).ToArray();
        return new PageResult<DepartmentDetailsData>(details, hasMore && values.Count > 0 ? values[^1].Id.ToString() : null);
    }

    public async Task<DepartmentOverviewDto?> GetOverviewAsync(Guid organizationId, Guid? departmentId, CancellationToken cancellationToken)
    {
        var units = await db.Departments.AsNoTracking().Where(x => x.OrganizationId == organizationId).ToListAsync(cancellationToken);
        var candidates = units.Where(x => x.Type == OrganizationUnitType.Department).ToArray();
        if (candidates.Length == 0) return null;

        var children = units.Where(x => x.ParentDepartmentId.HasValue)
            .GroupBy(x => x.ParentDepartmentId!.Value).ToDictionary(x => x.Key, x => x.ToArray());
        HashSet<Guid> DescendantIds(Guid departmentId)
        {
            var result = new HashSet<Guid>();
            var pending = new Stack<Guid>();
            pending.Push(departmentId);
            while (pending.TryPop(out var id) && result.Add(id))
                foreach (var child in children.GetValueOrDefault(id) ?? []) pending.Push(child.Id);
            return result;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentAssignments = await db.PositionAssignments.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today))
            .ToListAsync(cancellationToken);
        var department = departmentId is { } requestedId
            ? candidates.SingleOrDefault(x => x.Id == requestedId)
            : candidates
                .OrderByDescending(x => currentAssignments.Where(a => DescendantIds(x.Id).Contains(a.DepartmentId)).Select(a => a.EmployeeId).Distinct().Count())
                .ThenBy(x => x.CreatedAtUtc)
                .First();
        if (department is null) return null;
        var includedUnitIds = DescendantIds(department.Id);
        var allowedEmployeeIds = (await dataScope.GetAccessibleEmployeeIdsAsync(cancellationToken)).ToHashSet();
        var assignments = currentAssignments.Where(x => includedUnitIds.Contains(x.DepartmentId) && allowedEmployeeIds.Contains(x.EmployeeId))
            .GroupBy(x => x.EmployeeId).Select(x => x.OrderByDescending(a => a.EffectiveFrom).First()).ToArray();
        var employeeIds = assignments.Select(x => x.EmployeeId).ToArray();
        var positionIds = assignments.Select(x => x.PositionId).Distinct().ToArray();
        var employees = await db.Employees.AsNoTracking().Where(x => employeeIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var positions = await db.Positions.AsNoTracking().Where(x => positionIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken);
        var employmentRows = await db.Employments.AsNoTracking()
            .Where(x => employeeIds.Contains(x.EmployeeId) && x.StartDate <= today && (x.EndDate == null || x.EndDate >= today))
            .ToListAsync(cancellationToken);
        var employmentStatuses = employmentRows.GroupBy(x => x.EmployeeId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(e => e.StartDate).First().Status);
        var projectRows = await (from membership in db.ProjectMemberships.AsNoTracking()
                                 join project in db.Projects.AsNoTracking() on membership.ProjectId equals project.Id
                                 where employeeIds.Contains(membership.EmployeeId) && membership.EffectiveFrom <= today &&
                                       (membership.EffectiveTo == null || membership.EffectiveTo >= today) && project.Status == ProjectStatus.Active
                                 select new { membership.EmployeeId, project.Name }).ToListAsync(cancellationToken);
        var projects = projectRows.GroupBy(x => x.EmployeeId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Select(p => p.Name).Distinct().Order().ToArray());

        var leadershipIds = new[] { department.ManagerEmployeeId, department.HrEmployeeId, department.ProductManagerEmployeeId }
            .OfType<Guid>().Distinct().ToArray();
        var leaders = await db.Employees.AsNoTracking().Where(x => leadershipIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FirstName + " " + x.LastName, cancellationToken);
        var directCount = assignments.Count(x => x.DepartmentId == department.Id);
        var departmentDto = new DepartmentDto(
            department.Id, department.Name, department.Code, department.Type, department.ParentDepartmentId,
            department.ManagerEmployeeId, department.ManagerEmployeeId is { } managerId ? leaders.GetValueOrDefault(managerId) : null,
            department.HrEmployeeId, department.HrEmployeeId is { } hrId ? leaders.GetValueOrDefault(hrId) : null,
            department.ProductManagerEmployeeId, department.ProductManagerEmployeeId is { } productManagerId ? leaders.GetValueOrDefault(productManagerId) : null,
            department.Description, directCount, assignments.Length, children.GetValueOrDefault(department.Id)?.Length ?? 0, department.Version);
        var employeeDtos = assignments.Where(x => employees.ContainsKey(x.EmployeeId)).Select(assignment =>
        {
            var employee = employees[assignment.EmployeeId];
            return new DepartmentEmployeeDto(employee.Id, employee.EmployeeNumber, employee.FullName, employee.WorkEmail,
                positions.GetValueOrDefault(assignment.PositionId), employmentStatuses.GetValueOrDefault(employee.Id),
                projects.GetValueOrDefault(employee.Id) ?? []);
        }).OrderBy(x => x.FullName).ToArray();
        return new DepartmentOverviewDto(departmentDto, employeeDtos);
    }

    public Task<Department?> GetAsync(Guid organizationId, Guid id, CancellationToken cancellationToken) =>
        db.Departments.SingleOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId, cancellationToken);

    public void Add(Department department) => db.Departments.Add(department);

    public Task<bool> ExistsAsync(Guid organizationId, Guid id, CancellationToken cancellationToken) =>
        db.Departments.AnyAsync(x => x.Id == id && x.OrganizationId == organizationId, cancellationToken);

    public async Task<bool> WouldCreateCycleAsync(Guid organizationId, Guid departmentId, Guid parentDepartmentId, CancellationToken cancellationToken)
    {
        var visited = new HashSet<Guid> { departmentId };
        Guid? currentId = parentDepartmentId;
        while (currentId is { } id)
        {
            if (!visited.Add(id)) return true;
            currentId = await db.Departments.Where(x => x.OrganizationId == organizationId && x.Id == id)
                .Select(x => x.ParentDepartmentId).SingleOrDefaultAsync(cancellationToken);
        }
        return false;
    }

    public async Task<bool> HasIncompatibleChildrenAsync(Guid organizationId, Guid departmentId, OrganizationUnitType type, CancellationToken cancellationToken)
    {
        var childTypes = await db.Departments.Where(x => x.OrganizationId == organizationId && x.ParentDepartmentId == departmentId)
            .Select(x => x.Type).ToListAsync(cancellationToken);
        return childTypes.Any(childType => !Department.CanContain(type, childType));
    }

    public Task<bool> HasChildrenAsync(Guid organizationId, Guid departmentId, CancellationToken cancellationToken) =>
        db.Departments.AnyAsync(x => x.OrganizationId == organizationId && x.ParentDepartmentId == departmentId, cancellationToken);

    public Task<bool> HasAssignmentsAsync(Guid organizationId, Guid departmentId, CancellationToken cancellationToken) =>
        db.PositionAssignments.AnyAsync(x => x.OrganizationId == organizationId && x.DepartmentId == departmentId, cancellationToken);

    public Task<int> CountActiveDepartmentsAsync(Guid organizationId, CancellationToken cancellationToken) =>
        db.Departments.CountAsync(x => x.OrganizationId == organizationId && x.Type == OrganizationUnitType.Department, cancellationToken);
}
