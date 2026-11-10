using HrManagement.Api.Configuration;
using HrManagement.Application.Features.AuditEvents.Queries.GetEmployeeActivity;
using HrManagement.Application.Features.Employees.Commands.AddPositionAssignment;
using HrManagement.Application.Features.Employees.Commands.AddReportingLine;
using HrManagement.Application.Features.Employees.Commands.ArchiveEmployee;
using HrManagement.Application.Features.Employees.Commands.CreateEmployee;
using HrManagement.Application.Features.Employees.Commands.SetEmployment;
using HrManagement.Application.Features.Employees.Commands.UpdateEmployee;
using HrManagement.Application.Features.Employees.Queries.GetEmployeeById;
using HrManagement.Application.Features.Employees.Queries.GetEmployees;
using HrManagement.Application.Features.Employees.Queries.GetEmployeeHistory;
using HrManagement.Application.Features.Employees.Queries.GetEmployeeProfile;
using HrManagement.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

[Route("api/v1/employees"), Authorize(Policy = Policies.PeopleRead)]
public sealed class EmployeesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] Guid? departmentId, [FromQuery] EmploymentStatus? status, [FromQuery] string? cursor, [FromQuery] int limit = 50, CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetEmployeesQuery(search, departmentId, status, cursor, limit), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEmployeeByIdQuery(id), cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpGet("{id:guid}/profile")]
    public async Task<IActionResult> Profile(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetEmployeeProfileQuery(id), cancellationToken));

    [HttpGet("{id:guid}/employment-history")]
    public async Task<IActionResult> EmploymentHistory(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetEmploymentHistoryQuery(id), cancellationToken));

    [HttpGet("{id:guid}/assignments")]
    public async Task<IActionResult> AssignmentHistory(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetAssignmentHistoryQuery(id), cancellationToken));

    [HttpGet("{id:guid}/reporting-lines")]
    public async Task<IActionResult> ReportingLineHistory(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetReportingLineHistoryQuery(id), cancellationToken));

    [HttpGet("{id:guid}/direct-reports")]
    public async Task<IActionResult> DirectReports(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetDirectReportsQuery(id), cancellationToken));

    [HttpGet("{id:guid}/activity")]
    public async Task<IActionResult> Activity(Guid id, [FromQuery] string? cursor, [FromQuery] int limit = 30, CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetEmployeeActivityQuery(id, cursor, limit), cancellationToken));

    [HttpPost, Authorize(Policy = Policies.PeopleWrite)]
    public async Task<IActionResult> Create(CreateEmployeeCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedWithEtag($"/api/v1/employees/{result.Id}", result, result.Version);
    }

    [HttpPut("{id:guid}"), Authorize(Policy = Policies.PeopleWrite)]
    public async Task<IActionResult> Update(Guid id, UpdateEmployeeCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { Id = id, ExpectedVersion = ExpectedVersion() }, cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpDelete("{id:guid}"), Authorize(Policy = Policies.PeopleWrite)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new ArchiveEmployeeCommand(id, ExpectedVersion()), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/employment"), Authorize(Policy = Policies.PeopleWrite)]
    public async Task<IActionResult> SetEmployment(Guid id, SetEmploymentCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { EmployeeId = id }, cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpPost("{id:guid}/assignments"), Authorize(Policy = Policies.PeopleWrite)]
    public async Task<IActionResult> AddAssignment(Guid id, AddPositionAssignmentCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { EmployeeId = id }, cancellationToken);
        return CreatedWithEtag($"/api/v1/employees/{id}", result, result.Version);
    }

    [HttpPost("{id:guid}/reporting-lines"), Authorize(Policy = Policies.PeopleWrite)]
    public async Task<IActionResult> AddReportingLine(Guid id, AddReportingLineCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { EmployeeId = id }, cancellationToken);
        return CreatedWithEtag($"/api/v1/employees/{id}", result, result.Version);
    }
}
