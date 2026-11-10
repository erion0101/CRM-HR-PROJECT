using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Employees;

namespace HrManagement.Application.Features.Contracts.Commands.ActivateContract;

public sealed class ActivateContractCommandHandler(IContractRepository contracts, IEmployeeRepository employees, IUnitOfWork unitOfWork)
    : ICommandHandler<ActivateContractCommand, ContractDto>
{
    public async Task<ContractDto> Handle(ActivateContractCommand request, CancellationToken cancellationToken)
    {
        if (!await employees.ExistsAccessibleAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        var contract = await contracts.GetAsync(request.EmployeeId, request.ContractId, cancellationToken) ?? throw new NotFoundException("Contract not found.");
        if (contract.Version != request.ExpectedVersion) throw new ConflictException("The record was changed by another user.");
        contract.Activate(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return contract.ToDto();
    }
}
