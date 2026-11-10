using System.Security.Claims;
using HrManagement.Application.Common.Authorization;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Models;
using HrManagement.Application.Features.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Identity;

internal sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    AppDbContext db,
    ICurrentUserService currentUser,
    IAuditWriter audit) : IIdentityService
{
    public async Task<bool> SignInAsync(string email, string password, bool rememberMe, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null || !user.IsActive) return false;
        return (await signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true)).Succeeded;
    }

    public Task SignOutAsync() => signInManager.SignOutAsync();

    public async Task<CurrentUser?> GetCurrentAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } id) return null;
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null || !user.IsActive) return null;
        return new CurrentUser(user.Id, user.OrganizationId, user.EmployeeId, user.Email ?? string.Empty, (await userManager.GetRolesAsync(user)).ToArray());
    }

    public async Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } id) throw new ForbiddenException();
        var user = await userManager.FindByIdAsync(id.ToString()) ?? throw new NotFoundException("User not found.");
        EnsureSucceeded(await userManager.ChangePasswordAsync(user, currentPassword, newPassword));
    }

    public async Task<UserDto> CreateAsync(string email, string temporaryPassword, string role, Guid? employeeId, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        if (employeeId is { } id && !await db.Employees.AnyAsync(x => x.Id == id && x.OrganizationId == organizationId, cancellationToken))
            throw new ValidationException(new Dictionary<string, string[]> { ["employeeId"] = ["Employee does not exist."] });
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = new ApplicationUser { Id = Guid.CreateVersion7(), OrganizationId = organizationId, EmployeeId = employeeId, UserName = normalizedEmail, Email = normalizedEmail, EmailConfirmed = true, IsActive = true };
        EnsureSucceeded(await userManager.CreateAsync(user, temporaryPassword));
        EnsureSucceeded(await userManager.AddToRoleAsync(user, role));
        await AddIdentityClaimsAsync(user);
        await audit.RecordAccessAsync("Create", nameof(ApplicationUser), user.Id.ToString(), cancellationToken);
        return await MapAsync(user);
    }

    public async Task<UserDto> UpdateAsync(Guid id, bool isActive, string role, Guid? employeeId, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var user = await userManager.Users.SingleOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId, cancellationToken) ?? throw new NotFoundException("User not found.");
        if (employeeId is { } employee && !await db.Employees.AnyAsync(x => x.Id == employee && x.OrganizationId == organizationId, cancellationToken))
            throw new ValidationException(new Dictionary<string, string[]> { ["employeeId"] = ["Employee does not exist."] });
        user.IsActive = isActive;
        user.EmployeeId = employeeId;
        EnsureSucceeded(await userManager.UpdateAsync(user));
        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count > 0) EnsureSucceeded(await userManager.RemoveFromRolesAsync(user, roles));
        EnsureSucceeded(await userManager.AddToRoleAsync(user, role));
        var claims = await userManager.GetClaimsAsync(user);
        if (claims.Count > 0) EnsureSucceeded(await userManager.RemoveClaimsAsync(user, claims));
        await AddIdentityClaimsAsync(user);
        EnsureSucceeded(await userManager.UpdateSecurityStampAsync(user));
        await audit.RecordAccessAsync("Update", nameof(ApplicationUser), user.Id.ToString(), cancellationToken);
        return await MapAsync(user);
    }

    public async Task<PageResult<UserDto>> ListAsync(string? cursor, int limit, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var query = userManager.Users.Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id);
        if (Guid.TryParse(cursor, out var after)) query = query.Where(x => x.Id.CompareTo(after) > 0).OrderBy(x => x.Id);
        var users = await query.Take(limit + 1).ToListAsync(cancellationToken);
        var hasMore = users.Count > limit;
        if (hasMore) users.RemoveAt(users.Count - 1);
        var result = new List<UserDto>(users.Count);
        foreach (var user in users) result.Add(await MapAsync(user));
        return new PageResult<UserDto>(result, hasMore && users.Count > 0 ? users[^1].Id.ToString() : null);
    }

    public async Task SetScopesAsync(Guid id, IReadOnlyList<UserScopeInput> scopes, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        if (!await userManager.Users.AnyAsync(x => x.Id == id && x.OrganizationId == organizationId, cancellationToken)) throw new NotFoundException("User not found.");
        foreach (var scope in scopes.Where(x => x.ScopeType == HrManagement.Domain.ScopeType.Department))
            if (scope.ScopeEntityId is not { } departmentId || !await db.Departments.AnyAsync(x => x.Id == departmentId && x.OrganizationId == organizationId, cancellationToken))
                throw new ValidationException(new Dictionary<string, string[]> { ["scopes"] = ["Every organization-unit scope must reference an existing unit."] });
        var existing = await db.UserScopes.Where(x => x.UserId == id && x.OrganizationId == organizationId).ToListAsync(cancellationToken);
        db.UserScopes.RemoveRange(existing);
        foreach (var scope in scopes.Distinct()) db.UserScopes.Add(HrManagement.Domain.UserScope.Create(organizationId, id, scope.ScopeType, scope.ScopeEntityId));
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAccessAsync("UpdateScopes", nameof(ApplicationUser), id.ToString(), cancellationToken);
    }

    private async Task AddIdentityClaimsAsync(ApplicationUser user)
    {
        var claims = new List<Claim> { new("organization_id", user.OrganizationId.ToString()) };
        if (user.EmployeeId is { } employeeId) claims.Add(new Claim("employee_id", employeeId.ToString()));
        EnsureSucceeded(await userManager.AddClaimsAsync(user, claims));
    }

    private async Task<UserDto> MapAsync(ApplicationUser user) => new(user.Id, user.Email ?? string.Empty, user.IsActive, user.EmployeeId, (await userManager.GetRolesAsync(user)).ToArray());
    private Guid RequireOrganization() => currentUser.OrganizationId ?? throw new ForbiddenException();

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded) throw new ValidationException(result.Errors.GroupBy(x => x.Code).ToDictionary(x => x.Key, x => x.Select(e => e.Description).ToArray()));
    }
}
