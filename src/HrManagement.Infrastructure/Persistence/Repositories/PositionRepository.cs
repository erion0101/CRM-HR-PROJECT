using HrManagement.Application.Common.Models;
using HrManagement.Application.Features.Positions;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Repositories;

internal sealed class PositionRepository(AppDbContext db) : IPositionRepository
{
    public async Task<PageResult<Position>> ListAsync(Guid organizationId, string? cursor, int limit, CancellationToken cancellationToken)
    {
        var query = db.Positions.AsNoTracking().Where(x => x.OrganizationId == organizationId && x.DepartmentId != null).OrderBy(x => x.Id);
        if (Guid.TryParse(cursor, out var after)) query = query.Where(x => x.Id.CompareTo(after) > 0).OrderBy(x => x.Id);
        var values = await query.Take(limit + 1).ToListAsync(cancellationToken);
        var hasMore = values.Count > limit;
        if (hasMore) values.RemoveAt(values.Count - 1);
        return new PageResult<Position>(values, hasMore && values.Count > 0 ? values[^1].Id.ToString() : null);
    }

    public Task<Position?> GetAsync(Guid organizationId, Guid id, CancellationToken cancellationToken) =>
        db.Positions.SingleOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId, cancellationToken);

    public void Add(Position position) => db.Positions.Add(position);

    public Task<bool> ExistsAsync(Guid organizationId, Guid id, CancellationToken cancellationToken) =>
        db.Positions.AnyAsync(x => x.Id == id && x.OrganizationId == organizationId, cancellationToken);

    public Task<bool> BelongsToDepartmentAsync(Guid organizationId, Guid id, Guid departmentId, CancellationToken cancellationToken) =>
        db.Positions.AnyAsync(x => x.Id == id && x.OrganizationId == organizationId && x.DepartmentId == departmentId, cancellationToken);
}
