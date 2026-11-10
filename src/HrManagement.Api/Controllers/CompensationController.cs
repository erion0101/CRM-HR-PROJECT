using HrManagement.Api.Configuration;
using HrManagement.Application.Features.Compensation.Commands.AddCompensation;
using HrManagement.Application.Features.Compensation.Queries.GetEmployeeCompensation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

[Route("api/v1/employees/{employeeId:guid}/compensation"), Authorize(Policy = Policies.Salary)]
public sealed class CompensationController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid employeeId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetEmployeeCompensationQuery(employeeId), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Add(Guid employeeId, AddCompensationCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { EmployeeId = employeeId }, cancellationToken);
        return CreatedWithEtag($"/api/v1/employees/{employeeId}/compensation/{result.Id}", result, result.Version);
    }
}
