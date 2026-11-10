using HrManagement.Api.Configuration;
using HrManagement.Application.Features.Contracts.Commands.ActivateContract;
using HrManagement.Application.Features.Contracts.Commands.CreateContract;
using HrManagement.Application.Features.Contracts.Commands.ReviseContract;
using HrManagement.Application.Features.Contracts.Commands.TerminateContract;
using HrManagement.Application.Features.Contracts.Queries.GetEmployeeContracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

[Route("api/v1/employees/{employeeId:guid}/contracts"), Authorize(Policy = Policies.PeopleRead)]
public sealed class ContractsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid employeeId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetEmployeeContractsQuery(employeeId), cancellationToken));

    [HttpPost, Authorize(Policy = Policies.ContractsWrite)]
    public async Task<IActionResult> Create(Guid employeeId, CreateContractCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { EmployeeId = employeeId }, cancellationToken);
        return CreatedWithEtag($"/api/v1/employees/{employeeId}/contracts/{result.Id}", result, result.Version);
    }

    [HttpPost("{contractId:guid}/revisions"), Authorize(Policy = Policies.ContractsWrite)]
    public async Task<IActionResult> Revise(Guid employeeId, Guid contractId, ReviseContractCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { EmployeeId = employeeId, ContractId = contractId, ExpectedVersion = ExpectedVersion() }, cancellationToken);
        return CreatedWithEtag($"/api/v1/employees/{employeeId}/contracts/{contractId}", result, result.Version);
    }

    [HttpPost("{contractId:guid}/activate"), Authorize(Policy = Policies.ContractsWrite)]
    public async Task<IActionResult> Activate(Guid employeeId, Guid contractId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ActivateContractCommand(employeeId, contractId, ExpectedVersion()), cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpPost("{contractId:guid}/terminate"), Authorize(Policy = Policies.ContractsWrite)]
    public async Task<IActionResult> Terminate(Guid employeeId, Guid contractId, TerminateContractCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { EmployeeId = employeeId, ContractId = contractId, ExpectedVersion = ExpectedVersion() }, cancellationToken);
        return WithEtag(result, result.Version);
    }
}
