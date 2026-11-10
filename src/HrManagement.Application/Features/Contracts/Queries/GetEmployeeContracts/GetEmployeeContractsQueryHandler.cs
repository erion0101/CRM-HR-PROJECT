using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Employees;

namespace HrManagement.Application.Features.Contracts.Queries.GetEmployeeContracts;

public sealed class GetEmployeeContractsQueryHandler(IContractRepository contracts, IEmployeeRepository employees)
    : IQueryHandler<GetEmployeeContractsQuery, IReadOnlyList<ContractDto>>
{
    public async Task<IReadOnlyList<ContractDto>> Handle(GetEmployeeContractsQuery request, CancellationToken cancellationToken)
    {
        if (!await employees.ExistsAccessibleAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        return (await contracts.ListAsync(request.EmployeeId, cancellationToken)).Select(x => x.ToDto()).ToArray();
    }
}
