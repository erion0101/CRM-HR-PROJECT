using HrManagement.Api.Configuration;
using HrManagement.Application.Features.Projects.Commands.AddProjectMembership;
using HrManagement.Application.Features.Projects.Commands.ArchiveProject;
using HrManagement.Application.Features.Projects.Commands.CreateProject;
using HrManagement.Application.Features.Projects.Commands.EndProjectMembership;
using HrManagement.Application.Features.Projects.Commands.UpdateProject;
using HrManagement.Application.Features.Projects.Queries.GetProject;
using HrManagement.Application.Features.Projects.Queries.GetProjectEmployeeOptions;
using HrManagement.Application.Features.Projects.Queries.GetProjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

[Route("api/v1/projects"), Authorize]
public sealed class ProjectsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await sender.Send(new GetProjectsQuery(), cancellationToken));

    [HttpGet("employee-options"), Authorize(Policy = Policies.ProjectCreate)]
    public async Task<IActionResult> EmployeeOptions(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetProjectEmployeeOptionsQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProjectQuery(id), cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpPost, Authorize(Policy = Policies.ProjectCreate)]
    public async Task<IActionResult> Create(CreateProjectCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedWithEtag($"/api/v1/projects/{result.Id}", result, result.Version);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateProjectCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { Id = id, ExpectedVersion = ExpectedVersion() }, cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new ArchiveProjectCommand(id, ExpectedVersion()), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMember(Guid id, AddProjectMembershipCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { ProjectId = id }, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/members/{membershipId:guid}")]
    public async Task<IActionResult> EndMember(Guid id, Guid membershipId, [FromQuery] DateOnly effectiveTo, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new EndProjectMembershipCommand(id, membershipId, effectiveTo, ExpectedVersion()), cancellationToken);
        return Ok(result);
    }
}
