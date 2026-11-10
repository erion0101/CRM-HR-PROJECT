using HrManagement.Application.Features.Compensation;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Repositories;

internal sealed class CompensationRepository(AppDbContext db) : ICompensationRepository
{
    public async Task<IReadOnlyList<CompensationRecord>> ListAsync(Guid employeeId, CancellationToken cancellationToken) =>
        await db.CompensationRecords.Where(x => x.EmployeeId == employeeId).OrderByDescending(x => x.EffectiveFrom).ToListAsync(cancellationToken);

    public Task<bool> HasOverlapAsync(Guid employeeId, DateOnly from, DateOnly? to, CancellationToken cancellationToken) =>
        db.CompensationRecords.AnyAsync(x => x.EmployeeId == employeeId && x.EffectiveFrom <= (to ?? DateOnly.MaxValue) && from <= (x.EffectiveTo ?? DateOnly.MaxValue), cancellationToken);

    public void Add(CompensationRecord compensation) => db.CompensationRecords.Add(compensation);
}
