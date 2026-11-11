using HrManagement.Api.Configuration;
using HrManagement.Application.Features.Users.Commands.CreateUser;
using HrManagement.Application.Features.Users.Commands.SetUserScopes;
using HrManagement.Application.Features.Users.Commands.UpdateUser;
using HrManagement.Application.Features.Users.Queries.GetEmployeeLogin;
using HrManagement.Application.Features.Users.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

[Route("api/v1/users"), Authorize(Policy = Policies.Admin)]
public sealed class UsersController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? cursor, [FromQuery] int limit = 50, [FromQuery] bool unlinkedOnly = false,
        CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetUsersQuery(cursor, limit, unlinkedOnly), cancellationToken));

    /// <summary>The login that belongs to one employee, for the Login tab on their profile.</summary>
    [HttpGet("by-employee/{employeeId:guid}")]
    public async Task<IActionResult> ForEmployee(Guid employeeId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetEmployeeLoginQuery(employeeId), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Created($"/api/v1/users/{result.Id}", result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserCommand command, CancellationToken cancellationToken) =>
        Ok(await sender.Send(command with { Id = id }, cancellationToken));

    [HttpPut("{id:guid}/scopes")]
    public async Task<IActionResult> SetScopes(Guid id, SetUserScopesCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command with { UserId = id }, cancellationToken);
        return NoContent();
    }
}
