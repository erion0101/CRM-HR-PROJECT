using HrManagement.Api.Configuration;
using HrManagement.Application.Features.Ai.Commands.TestAiProvider;
using HrManagement.Application.Features.Ai.Queries.GetAiSettings;
using HrManagement.Application.Features.Ai.Queries.GetAiUsageSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

[Route("api/v1/administration"), Authorize(Policy = Policies.AiAdmin)]
public sealed class AiAdministrationController(ISender sender) : ApiControllerBase
{
    [HttpGet("ai-settings")]
    public async Task<IActionResult> Settings(CancellationToken cancellationToken) => Ok(await sender.Send(new GetAiSettingsQuery(), cancellationToken));

    [HttpGet("ai-usage")]
    public async Task<IActionResult> Usage(CancellationToken cancellationToken) => Ok(await sender.Send(new GetAiUsageSummaryQuery(), cancellationToken));

    [HttpPost("ai-settings/test")]
    public async Task<IActionResult> Test(CancellationToken cancellationToken) => Ok(new { connected = await sender.Send(new TestAiProviderCommand(), cancellationToken) });
}
