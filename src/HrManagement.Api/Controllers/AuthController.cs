using HrManagement.Application.Features.Authentication.Commands.ChangePassword;
using HrManagement.Application.Features.Authentication.Commands.Login;
using HrManagement.Application.Features.Authentication.Commands.Logout;
using HrManagement.Application.Features.Authentication.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

[Route("api/v1/auth")]
public sealed class AuthController(ISender sender, IAntiforgery antiforgery) : ApiControllerBase
{
    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken) =>
        await sender.Send(command, cancellationToken) ? NoContent() : Problem(statusCode: 401, title: "Invalid credentials");

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutCommand(), cancellationToken);
        return NoContent();
    }

    [HttpGet("me"), Authorize]
    public async Task<IActionResult> Current(CancellationToken cancellationToken) =>
        await sender.Send(new GetCurrentUserQuery(), cancellationToken) is { } user ? Ok(user) : Unauthorized();

    [HttpPut("password"), Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("antiforgery"), Authorize]
    public IActionResult Antiforgery()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken, headerName = "X-CSRF-TOKEN" });
    }
}
