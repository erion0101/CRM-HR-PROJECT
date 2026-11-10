using System.Text.Json;
using System.Text.Json.Serialization;
using HrManagement.Api.Configuration;
using HrManagement.Application.Features.Ai.Commands.CreateAiConversation;
using HrManagement.Application.Features.Ai.Commands.DeleteAiConversation;
using HrManagement.Application.Features.Ai.Commands.SendAiMessage;
using HrManagement.Application.Features.Ai.Queries.GetAiCapabilities;
using HrManagement.Application.Features.Ai.Queries.GetAiConversation;
using HrManagement.Application.Features.Ai.Queries.GetAiConversations;
using HrManagement.Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

[Route("api/v1/ai"), Authorize(Policy = Policies.AiUse)]
public sealed class AiController(ISender sender) : ApiControllerBase
{
    private static readonly JsonSerializerOptions StreamJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [HttpGet("capabilities")]
    public async Task<IActionResult> Capabilities(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetAiCapabilitiesQuery(), cancellationToken));

    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetAiConversationsQuery(), cancellationToken));

    [HttpPost("conversations")]
    public async Task<IActionResult> Create([FromBody] CreateConversationRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateAiConversationCommand(request.Title, request.ContextType, request.ContextId), cancellationToken);
        return Created($"/api/v1/ai/conversations/{result.Id}", result);
    }

    [HttpGet("conversations/{conversationId:guid}")]
    public async Task<IActionResult> Conversation(Guid conversationId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetAiConversationQuery(conversationId), cancellationToken));

    [HttpDelete("conversations/{conversationId:guid}")]
    public async Task<IActionResult> Delete(Guid conversationId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteAiConversationCommand(conversationId), cancellationToken);
        return NoContent();
    }

    [HttpPost("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> Send(Guid conversationId, [FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var wantsStream = Request.GetTypedHeaders().Accept?.Any(x => string.Equals(x.MediaType.Value, "text/event-stream", StringComparison.OrdinalIgnoreCase)) == true;
        if (!wantsStream)
            return Ok(await sender.Send(new SendAiMessageCommand(conversationId, request.Message), cancellationToken));

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache, no-store";
        Response.Headers.Append("X-Accel-Buffering", "no");
        await EventAsync("start", new { conversationId }, cancellationToken);
        await EventAsync("status", new { message = "Checking authorized HR data…" }, cancellationToken);
        try
        {
            var result = await sender.Send(new SendAiMessageCommand(conversationId, request.Message), cancellationToken);
            await EventAsync("status", new { message = "Preparing the authorized answer and sources…" }, cancellationToken);
            await EventAsync("delta", new { text = result.Message }, cancellationToken);
            await EventAsync("result", result.Result, cancellationToken);
            await EventAsync("sources", result.Sources, cancellationToken);
            await EventAsync("done", result, cancellationToken);
        }
        catch (ValidationException exception) when (!cancellationToken.IsCancellationRequested)
        {
            var message = exception.Errors.SelectMany(x => x.Value).FirstOrDefault() ?? "The AI request was rejected.";
            await EventAsync("error", new { message, code = exception.Errors.ContainsKey("rateLimit") ? "rate_limit" : "validation" }, cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            await EventAsync("error", new { message = "The AI request could not be completed.", code = "unavailable" }, cancellationToken);
        }
        return new EmptyResult();
    }

    private async Task EventAsync(string name, object payload, CancellationToken cancellationToken)
    {
        await Response.WriteAsync($"event: {name}\ndata: {JsonSerializer.Serialize(payload, StreamJson)}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    public sealed record CreateConversationRequest(string? Title, string? ContextType, Guid? ContextId);
    public sealed record SendMessageRequest(string Message);
}
