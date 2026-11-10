using HrManagement.Application.Common.Models;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Positions;

public interface IPositionRepository
{
    Task<PageResult<Position>> ListAsync(Guid organizationId, string? cursor, int limit, CancellationToken cancellationToken);
    Task<Position?> GetAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);
    void Add(Position position);
    Task<bool> ExistsAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);
    Task<bool> BelongsToDepartmentAsync(Guid organizationId, Guid id, Guid departmentId, CancellationToken cancellationToken);
}
