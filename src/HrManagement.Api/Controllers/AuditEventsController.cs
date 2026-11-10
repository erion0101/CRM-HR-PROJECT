using HrManagement.Api.Configuration;
using HrManagement.Application.Features.AuditEvents.Queries.GetAuditEvents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

[Route("api/v1/audit-events"), Authorize(Policy = Policies.Audit)]
public sealed class AuditEventsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? entityType, [FromQuery] Guid? userId, [FromQuery] DateTimeOffset? from, [FromQuery] string? cursor, [FromQuery] int limit = 50, CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetAuditEventsQuery(entityType, userId, from, cursor, limit), cancellationToken));
}
