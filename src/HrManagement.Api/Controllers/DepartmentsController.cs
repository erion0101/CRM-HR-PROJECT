using HrManagement.Api.Configuration;
using HrManagement.Application.Features.Departments.Commands.ArchiveDepartment;
using HrManagement.Application.Features.Departments.Commands.CreateDepartment;
using HrManagement.Application.Features.Departments.Commands.UpdateDepartment;
using HrManagement.Application.Features.Departments.Commands.AssignDepartmentEmployee;
using HrManagement.Application.Features.Departments.Commands.RemoveDepartmentEmployee;
using HrManagement.Application.Features.Departments.Queries.GetCurrentDepartment;
using HrManagement.Application.Features.Departments.Queries.GetDepartments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

[Route("api/v1/departments"), Authorize(Policy = Policies.PeopleRead)]
public sealed class DepartmentsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? cursor, [FromQuery] int limit = 50, CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetDepartmentsQuery(cursor, limit), cancellationToken));

    [HttpGet("current")]
    public async Task<IActionResult> Current([FromQuery] Guid? departmentId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCurrentDepartmentQuery(departmentId), cancellationToken));

    [HttpPost, Authorize(Policy = Policies.OrganizationWrite)]
    public async Task<IActionResult> Create(CreateDepartmentCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedWithEtag($"/api/v1/departments/{result.Id}", result, result.Version);
    }

    [HttpPut("{id:guid}"), Authorize(Policy = Policies.OrganizationWrite)]
    public async Task<IActionResult> Update(Guid id, UpdateDepartmentCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { Id = id, ExpectedVersion = ExpectedVersion() }, cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpDelete("{id:guid}"), Authorize(Policy = Policies.OrganizationWrite)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new ArchiveDepartmentCommand(id, ExpectedVersion()), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/employees/{employeeId:guid}"), Authorize(Policy = Policies.OrganizationWrite)]
    public async Task<IActionResult> AssignEmployee(Guid id, Guid employeeId, AssignDepartmentEmployeeCommand command, CancellationToken cancellationToken) =>
        Ok(await sender.Send(command with { DepartmentId = id, EmployeeId = employeeId }, cancellationToken));

    [HttpDelete("{id:guid}/employees/{employeeId:guid}"), Authorize(Policy = Policies.OrganizationWrite)]
    public async Task<IActionResult> RemoveEmployee(Guid id, Guid employeeId, [FromQuery] DateOnly effectiveOn, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new RemoveDepartmentEmployeeCommand(id, employeeId, effectiveOn), cancellationToken));
}
