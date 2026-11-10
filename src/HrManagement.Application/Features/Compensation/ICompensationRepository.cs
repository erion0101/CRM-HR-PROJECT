using HrManagement.Domain;

namespace HrManagement.Application.Features.Compensation;

public interface ICompensationRepository
{
    Task<IReadOnlyList<CompensationRecord>> ListAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<bool> HasOverlapAsync(Guid employeeId, DateOnly from, DateOnly? to, CancellationToken cancellationToken);
    void Add(CompensationRecord compensation);
}
