using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Domain;

namespace HrManagement.Infrastructure.Auditing;

internal sealed class AuditWriter(AppDbContext db, ICurrentUserService currentUser) : IAuditWriter
{
    public async Task RecordAccessAsync(string action, string entityType, string entityId, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        db.AuditEvents.Add(AuditEvent.Create(organizationId, currentUser.UserId, currentUser.EmployeeId, action, entityType, entityId, null, currentUser.CorrelationId, currentUser.IpAddress));
        await db.SaveChangesAsync(cancellationToken);
    }
}
