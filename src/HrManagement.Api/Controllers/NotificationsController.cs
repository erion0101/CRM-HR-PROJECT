using HrManagement.Application.Features.Notifications.Commands.MarkAllNotificationsRead;
using HrManagement.Application.Features.Notifications.Commands.MarkNotificationRead;
using HrManagement.Application.Features.Notifications.Queries.GetMyNotifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

/// <summary>The signed-in user's own in-app notifications.</summary>
[Route("api/v1/notifications"), Authorize]
public sealed class NotificationsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Mine([FromQuery] int limit = 30, CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetMyNotificationsQuery(limit), cancellationToken));

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new MarkNotificationReadCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken) =>
        Ok(new { updated = await sender.Send(new MarkAllNotificationsReadCommand(), cancellationToken) });
}
