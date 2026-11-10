using HrManagement.Domain;

namespace HrManagement.Application.Features.Contracts;

public interface IContractRepository
{
    Task<IReadOnlyList<Contract>> ListAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<Contract?> GetAsync(Guid employeeId, Guid contractId, CancellationToken cancellationToken);
    Task<bool> HasOverlapAsync(Guid employeeId, Guid? excludedContractId, DateOnly start, DateOnly? end, CancellationToken cancellationToken);
    void Add(Contract contract);
    void AddVersion(ContractVersion version);
}
