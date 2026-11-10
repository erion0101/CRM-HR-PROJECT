using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Employees;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Contracts.Commands.CreateContract;

public sealed class CreateContractCommandHandler(IContractRepository contracts, IEmployeeRepository employees, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : ICommandHandler<CreateContractCommand, ContractDto>
{
    public async Task<ContractDto> Handle(CreateContractCommand request, CancellationToken cancellationToken)
    {
        if (!await employees.ExistsAccessibleAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        if (await contracts.HasOverlapAsync(request.EmployeeId, null, request.StartDate, request.EndDate, cancellationToken)) throw new ConflictException("Contract dates overlap an existing contract.");
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var contract = Contract.Create(organizationId, request.EmployeeId, request.ContractNumber, request.StartDate, request.EndDate);
        contracts.Add(contract);
        contracts.AddVersion(ContractVersion.Create(organizationId, contract.Id, 1, request.StartDate, request.EndDate, request.Notes));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return contract.ToDto();
    }
}
