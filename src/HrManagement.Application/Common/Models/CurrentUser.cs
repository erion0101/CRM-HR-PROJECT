namespace HrManagement.Application.Common.Models;

public sealed record CurrentUser(Guid UserId, Guid OrganizationId, Guid? EmployeeId, string Email, IReadOnlyList<string> Roles);
