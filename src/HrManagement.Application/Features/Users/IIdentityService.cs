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
    Task<PageResult<UserDto>> ListAsync(string? cursor, int limit, CancellationToken cancellationToken);
}
