namespace HrManagement.Application.Common.Interfaces;

public interface IAuthorizedDataScope
{
    Guid OrganizationId { get; }
    Task<IReadOnlyList<Guid>> GetAccessibleEmployeeIdsAsync(CancellationToken cancellationToken);
    Task<bool> CanAccessEmployeeAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<string> GetAuthorizationFingerprintAsync(CancellationToken cancellationToken);
}
