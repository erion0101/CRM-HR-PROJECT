namespace HrManagement.Application.Features.Users;

public sealed record UserDto(Guid Id, string Email, bool IsActive, Guid? EmployeeId, IReadOnlyList<string> Roles);
