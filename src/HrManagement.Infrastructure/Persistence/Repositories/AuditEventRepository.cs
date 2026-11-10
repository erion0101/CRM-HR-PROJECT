using HrManagement.Application.Common.Models;
using HrManagement.Application.Features.AuditEvents;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Repositories;

internal sealed class AuditEventRepository(AppDbContext db) : IAuditEventRepository
{
    public async Task<PageResult<AuditEventDto>> SearchAsync(Guid organizationId, string? entityType, Guid? userId, DateTimeOffset? from, string? cursor, int limit, CancellationToken cancellationToken)
    {
        var query = db.AuditEvents.AsNoTracking().Where(x => x.OrganizationId == organizationId);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType);
        if (userId is not null) query = query.Where(x => x.UserId == userId);
        if (from is not null) query = query.Where(x => x.OccurredAtUtc >= from);
        query = query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id);
        if (Guid.TryParse(cursor, out var after)) query = query.Where(x => x.Id.CompareTo(after) < 0).OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id);
        var values = await query.Take(limit + 1).ToListAsync(cancellationToken);
        var hasMore = values.Count > limit;
        if (hasMore) values.RemoveAt(values.Count - 1);
        return new PageResult<AuditEventDto>(await MapAsync(values, cancellationToken), hasMore && values.Count > 0 ? values[^1].Id.ToString() : null);
    }

    public async Task<PageResult<AuditEventDto>> SearchEmployeeActivityAsync(Guid organizationId, Guid employeeId, bool includeCompensation, string? cursor, int limit, CancellationToken cancellationToken)
    {
        var employmentIds = await IdStringsAsync(db.Employments.IgnoreQueryFilters().Where(x => x.OrganizationId == organizationId && x.EmployeeId == employeeId).Select(x => x.Id), cancellationToken);
        var assignmentIds = await IdStringsAsync(db.PositionAssignments.IgnoreQueryFilters().Where(x => x.OrganizationId == organizationId && x.EmployeeId == employeeId).Select(x => x.Id), cancellationToken);
        var reportingIds = await IdStringsAsync(db.ReportingLines.IgnoreQueryFilters().Where(x => x.OrganizationId == organizationId && x.EmployeeId == employeeId).Select(x => x.Id), cancellationToken);
        var contractGuids = await db.Contracts.IgnoreQueryFilters().Where(x => x.OrganizationId == organizationId && x.EmployeeId == employeeId).Select(x => x.Id).ToListAsync(cancellationToken);
        var contractIds = contractGuids.Select(x => x.ToString()).ToArray();
        var contractVersionIds = await IdStringsAsync(db.ContractVersions.Where(x => contractGuids.Contains(x.ContractId)).Select(x => x.Id), cancellationToken);
        var compensationIds = includeCompensation
            ? await IdStringsAsync(db.CompensationRecords.IgnoreQueryFilters().Where(x => x.OrganizationId == organizationId && x.EmployeeId == employeeId).Select(x => x.Id), cancellationToken)
            : [];
        var associations = await db.DocumentAssociations.Where(x => x.OrganizationId == organizationId && x.AssociationType == DocumentAssociationType.Employee && x.AssociatedEntityId == employeeId)
            .Select(x => new { x.Id, x.DocumentId }).ToListAsync(cancellationToken);
        var documentGuids = associations.Select(x => x.DocumentId).Distinct().ToArray();
        var documentIds = documentGuids.Select(x => x.ToString()).ToArray();
        var associationIds = associations.Select(x => x.Id.ToString()).ToArray();
        var documentVersionIds = await IdStringsAsync(db.DocumentVersions.Where(x => documentGuids.Contains(x.DocumentId)).Select(x => x.Id), cancellationToken);
        var employeeKey = employeeId.ToString();

        var query = db.AuditEvents.AsNoTracking().Where(x => x.OrganizationId == organizationId &&
            ((x.EntityType == nameof(Employee) && x.EntityId == employeeKey) ||
             (x.EntityType == nameof(Employment) && employmentIds.Contains(x.EntityId)) ||
             (x.EntityType == nameof(PositionAssignment) && assignmentIds.Contains(x.EntityId)) ||
             (x.EntityType == nameof(ReportingLine) && reportingIds.Contains(x.EntityId)) ||
             (x.EntityType == nameof(Contract) && contractIds.Contains(x.EntityId)) ||
             (x.EntityType == nameof(ContractVersion) && contractVersionIds.Contains(x.EntityId)) ||
             (includeCompensation && x.EntityType == nameof(CompensationRecord) && compensationIds.Contains(x.EntityId)) ||
             (x.EntityType == nameof(EmployeeDocument) && documentIds.Contains(x.EntityId)) ||
             (x.EntityType == nameof(DocumentVersion) && documentVersionIds.Contains(x.EntityId)) ||
             (x.EntityType == nameof(DocumentAssociation) && associationIds.Contains(x.EntityId))));
        query = query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id);
        if (Guid.TryParse(cursor, out var after))
            query = query.Where(x => x.Id.CompareTo(after) < 0).OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id);
        var values = await query.Take(limit + 1).ToListAsync(cancellationToken);
        var hasMore = values.Count > limit;
        if (hasMore) values.RemoveAt(values.Count - 1);
        return new PageResult<AuditEventDto>(await MapAsync(values, cancellationToken), hasMore && values.Count > 0 ? values[^1].Id.ToString() : null);
    }

    private async Task<IReadOnlyList<AuditEventDto>> MapAsync(IReadOnlyList<AuditEvent> values, CancellationToken cancellationToken)
    {
        var userIds = values.Where(x => x.UserId is not null).Select(x => x.UserId!.Value).Distinct().ToArray();
        var emails = await db.Users.Where(x => userIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Email, cancellationToken);
        return values.Select(x => new AuditEventDto(
            x.Id,
            x.OccurredAtUtc,
            x.UserId,
            x.EmployeeId,
            x.UserId is { } userId && emails.TryGetValue(userId, out var email) ? email : null,
            x.Action,
            x.EntityType,
            x.EntityId,
            x.CorrelationId,
            x.IpAddress)).ToArray();
    }

    private static async Task<string[]> IdStringsAsync(IQueryable<Guid> query, CancellationToken cancellationToken) =>
        (await query.ToListAsync(cancellationToken)).Select(x => x.ToString()).ToArray();
}
