using System.Security.Cryptography;
using System.Text;
using HrManagement.Application.Common.Authorization;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authorization;

internal sealed class AuthorizedDataScope(AppDbContext db, ICurrentUserService currentUser) : IAuthorizedDataScope
{
    public Guid OrganizationId => currentUser.OrganizationId ?? throw new ForbiddenException();

    public async Task<IReadOnlyList<Guid>> GetAccessibleEmployeeIdsAsync(CancellationToken cancellationToken)
    {
        var organizationId = OrganizationId;
        if (currentUser.IsInRole(Roles.Hr))
        {
            return await db.Employees.Where(x => x.OrganizationId == organizationId)
                .Select(x => x.Id).ToListAsync(cancellationToken);
        }

        if (currentUser.EmployeeId is not { } self)
        {
            return [];
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (currentUser.IsInRole(Roles.Manager))
        {
            var reports = await db.ReportingLines
                .Where(x => x.OrganizationId == organizationId && x.ManagerEmployeeId == self &&
                    x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today))
                .Select(x => x.EmployeeId).ToListAsync(cancellationToken);
            reports.Add(self);
            return reports.Distinct().ToArray();
        }

        if (!currentUser.IsInRole(Roles.Hr) || currentUser.UserId is not { } userId)
        {
            return [self];
        }

        var scopes = await db.UserScopes.Where(x => x.OrganizationId == organizationId && x.UserId == userId)
            .OrderBy(x => x.ScopeType).ThenBy(x => x.ScopeEntityId).ToListAsync(cancellationToken);
        if (scopes.Any(x => x.ScopeType == ScopeType.Organization))
        {
            return await db.Employees.Where(x => x.OrganizationId == organizationId)
                .Select(x => x.Id).ToListAsync(cancellationToken);
        }

        var allowed = new HashSet<Guid>();
        if (scopes.Any(x => x.ScopeType == ScopeType.Self))
        {
            allowed.Add(self);
        }

        if (scopes.Any(x => x.ScopeType == ScopeType.DirectReports))
        {
            var reports = await db.ReportingLines
                .Where(x => x.OrganizationId == organizationId && x.ManagerEmployeeId == self &&
                    x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today))
                .Select(x => x.EmployeeId).ToListAsync(cancellationToken);
            allowed.UnionWith(reports);
        }

        var departmentIds = scopes.Where(x => x.ScopeType == ScopeType.Department && x.ScopeEntityId.HasValue)
            .Select(x => x.ScopeEntityId!.Value).ToHashSet();
        if (departmentIds.Count > 0)
        {
            var hierarchy = await db.Departments.Where(x => x.OrganizationId == organizationId)
                .Select(x => new { x.Id, x.ParentDepartmentId }).ToListAsync(cancellationToken);
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var unit in hierarchy.Where(x => x.ParentDepartmentId.HasValue && departmentIds.Contains(x.ParentDepartmentId.Value)))
                {
                    changed |= departmentIds.Add(unit.Id);
                }
            }

            var employees = await db.PositionAssignments
                .Where(x => x.OrganizationId == organizationId && departmentIds.Contains(x.DepartmentId) &&
                    x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today))
                .Select(x => x.EmployeeId).Distinct().ToListAsync(cancellationToken);
            allowed.UnionWith(employees);
        }

        return allowed.Order().ToArray();
    }

    public async Task<bool> CanAccessEmployeeAsync(Guid employeeId, CancellationToken cancellationToken) =>
        (await GetAccessibleEmployeeIdsAsync(cancellationToken)).Contains(employeeId);

    public async Task<string> GetAuthorizationFingerprintAsync(CancellationToken cancellationToken)
    {
        var roles = Roles.All.Where(currentUser.IsInRole).Order(StringComparer.Ordinal);
        var employeeIds = await GetAccessibleEmployeeIdsAsync(cancellationToken);
        var source = string.Join('|', OrganizationId, currentUser.UserId, currentUser.EmployeeId,
            string.Join(',', roles), string.Join(',', employeeIds.Order()));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();
    }
}
