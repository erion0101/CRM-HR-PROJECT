namespace HrManagement.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? OrganizationId { get; }
    Guid? EmployeeId { get; }
    string? Email { get; }
    string? CorrelationId { get; }
    string? IpAddress { get; }
    bool IsInRole(string role);
}
