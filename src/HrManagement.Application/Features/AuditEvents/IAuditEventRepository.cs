using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.AuditEvents;

public interface IAuditEventRepository
{
    Task<PageResult<AuditEventDto>> SearchAsync(Guid organizationId, string? entityType, Guid? userId, DateTimeOffset? from, string? cursor, int limit, CancellationToken cancellationToken);
    Task<PageResult<AuditEventDto>> SearchEmployeeActivityAsync(Guid organizationId, Guid employeeId, bool includeCompensation, string? cursor, int limit, CancellationToken cancellationToken);
}
