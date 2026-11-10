using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Employees;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Contracts.Commands.ReviseContract;

public sealed class ReviseContractCommandHandler(IContractRepository contracts, IEmployeeRepository employees, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : ICommandHandler<ReviseContractCommand, ContractDto>
{
    public async Task<ContractDto> Handle(ReviseContractCommand request, CancellationToken cancellationToken)
    {
        if (!await employees.ExistsAccessibleAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        var contract = await contracts.GetAsync(request.EmployeeId, request.ContractId, cancellationToken) ?? throw new NotFoundException("Contract not found.");
        if (contract.Version != request.ExpectedVersion) throw new ConflictException("The record was changed by another user.");
        if (await contracts.HasOverlapAsync(request.EmployeeId, contract.Id, request.StartDate, request.EndDate, cancellationToken)) throw new ConflictException("Revised contract dates overlap another contract.");
        contract.AddRevision(request.StartDate, request.EndDate, DateTimeOffset.UtcNow);
        contracts.AddVersion(ContractVersion.Create(currentUser.OrganizationId ?? throw new ForbiddenException(), contract.Id, contract.CurrentRevision, request.StartDate, request.EndDate, request.Notes));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return contract.ToDto();
    }
}
