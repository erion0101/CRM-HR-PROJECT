using HrManagement.Application.Features.Contracts;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Repositories;

internal sealed class ContractRepository(AppDbContext db) : IContractRepository
{
    public async Task<IReadOnlyList<Contract>> ListAsync(Guid employeeId, CancellationToken cancellationToken) =>
        await db.Contracts.Where(x => x.EmployeeId == employeeId).OrderByDescending(x => x.StartDate).ToListAsync(cancellationToken);

    public Task<Contract?> GetAsync(Guid employeeId, Guid contractId, CancellationToken cancellationToken) =>
        db.Contracts.SingleOrDefaultAsync(x => x.Id == contractId && x.EmployeeId == employeeId, cancellationToken);

    public Task<bool> HasOverlapAsync(Guid employeeId, Guid? excludedContractId, DateOnly start, DateOnly? end, CancellationToken cancellationToken) =>
        db.Contracts.AnyAsync(x => x.EmployeeId == employeeId && (!excludedContractId.HasValue || x.Id != excludedContractId.Value) && x.StartDate <= (end ?? DateOnly.MaxValue) && start <= (x.EndDate ?? DateOnly.MaxValue), cancellationToken);

    public void Add(Contract contract) => db.Contracts.Add(contract);
    public void AddVersion(ContractVersion version) => db.ContractVersions.Add(version);
}
