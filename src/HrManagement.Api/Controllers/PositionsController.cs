using HrManagement.Api.Configuration;
using HrManagement.Application.Features.Positions.Commands.ArchivePosition;
using HrManagement.Application.Features.Positions.Commands.CreatePosition;
using HrManagement.Application.Features.Positions.Commands.UpdatePosition;
using HrManagement.Application.Features.Positions.Queries.GetPositions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

[Route("api/v1/positions"), Authorize(Policy = Policies.PeopleRead)]
public sealed class PositionsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? cursor, [FromQuery] int limit = 50, CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetPositionsQuery(cursor, limit), cancellationToken));

    [HttpPost, Authorize(Policy = Policies.OrganizationWrite)]
    public async Task<IActionResult> Create(CreatePositionCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedWithEtag($"/api/v1/positions/{result.Id}", result, result.Version);
    }

    [HttpPut("{id:guid}"), Authorize(Policy = Policies.OrganizationWrite)]
    public async Task<IActionResult> Update(Guid id, UpdatePositionCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { Id = id, ExpectedVersion = ExpectedVersion() }, cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpDelete("{id:guid}"), Authorize(Policy = Policies.OrganizationWrite)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new ArchivePositionCommand(id, ExpectedVersion()), cancellationToken);
        return NoContent();
    }
}
