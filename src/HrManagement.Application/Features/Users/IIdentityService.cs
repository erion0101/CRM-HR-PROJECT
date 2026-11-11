using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.Users;

public interface IIdentityService
{
    Task<bool> SignInAsync(string email, string password, bool rememberMe, CancellationToken cancellationToken);
    Task SignOutAsync();
    Task<CurrentUser?> GetCurrentAsync(CancellationToken cancellationToken);
    Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken);
    Task<UserDto> CreateAsync(string email, string temporaryPassword, string role, Guid? employeeId, CancellationToken cancellationToken);
    Task<UserDto> UpdateAsync(Guid id, bool isActive, string role, Guid? employeeId, CancellationToken cancellationToken);
    Task SetScopesAsync(Guid id, IReadOnlyList<UserScopeInput> scopes, CancellationToken cancellationToken);
    /// <param name="unlinkedOnly">Only logins that do not belong to an employee (such as the first admin account).</param>
    Task<PageResult<UserDto>> ListAsync(string? cursor, int limit, bool unlinkedOnly, CancellationToken cancellationToken);
    /// <summary>The login that belongs to this employee, or null when they have none.</summary>
    Task<UserDto?> GetByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken);
}
