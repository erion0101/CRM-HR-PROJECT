using HrManagement.Application.Common.Authorization;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Projects;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Repositories;

internal sealed class ProjectRepository(AppDbContext db, ICurrentUserService currentUser) : IProjectRepository
{
    private Guid OrganizationId => currentUser.OrganizationId ?? throw new ForbiddenException();

    public void Add(Project project) => db.Projects.Add(project);
    public void Add(ProjectMembership membership) => db.ProjectMemberships.Add(membership);
    public void Add(ProjectResourceLink link) => db.ProjectResourceLinks.Add(link);

    public Task<Project?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Projects.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.Id == id, cancellationToken);

    public Task<ProjectMembership?> GetMembershipAsync(Guid projectId, Guid membershipId, CancellationToken cancellationToken) =>
        db.ProjectMemberships.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.ProjectId == projectId && x.Id == membershipId, cancellationToken);

    public Task<bool> HasOverlappingMembershipAsync(Guid projectId, Guid employeeId, DateOnly from, DateOnly? to, CancellationToken cancellationToken) =>
        db.ProjectMemberships.AnyAsync(x => x.OrganizationId == OrganizationId && x.ProjectId == projectId && x.EmployeeId == employeeId &&
            (to == null || x.EffectiveFrom <= to) && (x.EffectiveTo == null || x.EffectiveTo >= from), cancellationToken);

    public Task<bool> CodeExistsAsync(string code, Guid? excludingId, CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return db.Projects.AnyAsync(x => x.OrganizationId == OrganizationId && x.Code == normalized && (!excludingId.HasValue || x.Id != excludingId), cancellationToken);
    }

    public async Task<bool> CanAccessAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(Roles.Hr) || currentUser.IsInRole(Roles.ProductManager)) return true;
        if (currentUser.EmployeeId is not { } employeeId) return false;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.Projects.AnyAsync(x => x.OrganizationId == OrganizationId && x.Id == projectId &&
            (x.LeadEmployeeId == employeeId || db.ProjectMemberships.Any(m => m.ProjectId == projectId && m.EmployeeId == employeeId &&
                m.EffectiveFrom <= today && (m.EffectiveTo == null || m.EffectiveTo >= today))), cancellationToken);
    }

    public async Task<bool> CanManageAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(Roles.Hr) || currentUser.IsInRole(Roles.ProductManager)) return true;
        return currentUser.EmployeeId is { } employeeId && await db.Projects.AnyAsync(
            x => x.OrganizationId == OrganizationId && x.Id == projectId && x.LeadEmployeeId == employeeId, cancellationToken);
    }

    public Task<bool> EmployeeExistsAsync(Guid employeeId, CancellationToken cancellationToken) =>
        db.Employees.AnyAsync(x => x.OrganizationId == OrganizationId && x.Id == employeeId, cancellationToken);

    public Task<bool> DepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken) =>
        db.Departments.AnyAsync(x => x.OrganizationId == OrganizationId && x.Id == departmentId, cancellationToken);

    public Task<bool> EmployeeHasActiveMembershipAsync(Guid projectId, Guid employeeId, DateOnly onDate, CancellationToken cancellationToken) =>
        db.ProjectMemberships.AnyAsync(x => x.OrganizationId == OrganizationId && x.ProjectId == projectId && x.EmployeeId == employeeId &&
            x.EffectiveFrom <= onDate && (x.EffectiveTo == null || x.EffectiveTo >= onDate), cancellationToken);

    public async Task<IReadOnlyList<ProjectDto>> ListVisibleAsync(CancellationToken cancellationToken)
    {
        var projects = await db.Projects.AsNoTracking().Where(x => x.OrganizationId == OrganizationId).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var result = new List<ProjectDto>();
        foreach (var project in projects)
        {
            if (await CanAccessAsync(project.Id, cancellationToken)) result.Add(await MapAsync(project, cancellationToken));
        }
        return result;
    }

    public async Task<IReadOnlyList<ProjectEmployeeOptionDto>> ListEmployeeOptionsAsync(CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(Roles.Hr) && !currentUser.IsInRole(Roles.ProductManager)) throw new ForbiddenException();
        return await db.Employees.AsNoTracking().Where(x => x.OrganizationId == OrganizationId)
            .OrderBy(x => x.FirstName).ThenBy(x => x.LastName).ThenBy(x => x.EmployeeNumber)
            .Select(x => new ProjectEmployeeOptionDto(x.Id, x.EmployeeNumber, x.FirstName + " " + x.LastName, x.WorkEmail))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectDto?> GetVisibleAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync(id, cancellationToken)) return null;
        var project = await db.Projects.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.Id == id, cancellationToken);
        return project is null ? null : await MapAsync(project, cancellationToken);
    }

    private async Task<ProjectDto> MapAsync(Project project, CancellationToken cancellationToken)
    {
        var employeeIds = new[] { project.LeadEmployeeId, project.OnboardingContactEmployeeId }.OfType<Guid>().Distinct().ToArray();
        var employees = await db.Employees.AsNoTracking().Where(x => employeeIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.FirstName + " " + x.LastName, cancellationToken);
        var departmentName = project.DepartmentId is { } departmentId
            ? await db.Departments.Where(x => x.Id == departmentId).Select(x => x.Name).SingleOrDefaultAsync(cancellationToken)
            : null;
        var members = await (from membership in db.ProjectMemberships.AsNoTracking()
                             join employee in db.Employees.AsNoTracking() on membership.EmployeeId equals employee.Id
                             where membership.OrganizationId == OrganizationId && membership.ProjectId == project.Id
                             orderby membership.EffectiveTo == null descending, employee.FirstName, employee.LastName
                             select new ProjectMemberDto(membership.Id, employee.Id, employee.FirstName + " " + employee.LastName,
                                 membership.ProjectRole, membership.Responsibilities, membership.EffectiveFrom, membership.EffectiveTo, membership.Version))
            .ToListAsync(cancellationToken);
        var links = await db.ProjectResourceLinks.AsNoTracking().Where(x => x.OrganizationId == OrganizationId && x.ProjectId == project.Id)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Label)
            .Select(x => new ProjectResourceLinkDto(x.Id, x.Label, x.Url, x.ResourceType, x.SortOrder)).ToListAsync(cancellationToken);
        return new ProjectDto(project.Id, project.Code, project.Name, project.Purpose, project.Description, project.DepartmentId, departmentName,
            project.LeadEmployeeId, employees.GetValueOrDefault(project.LeadEmployeeId) ?? "Unknown",
            project.OnboardingContactEmployeeId, project.OnboardingContactEmployeeId is { } contactId ? employees.GetValueOrDefault(contactId) : null,
            project.Status, await CanManageAsync(project.Id, cancellationToken), members, links, project.Version);
    }
}
