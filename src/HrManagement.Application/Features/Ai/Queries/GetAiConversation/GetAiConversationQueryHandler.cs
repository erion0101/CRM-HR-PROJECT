using System.Text.Json;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Ai.Queries.GetAiConversation;

public sealed class GetAiConversationQueryHandler(
    IAiConversationRepository repository,
    IAuthorizedDataScope scope,
    IAiSourceAuthorizationService sourceAuthorization) : IQueryHandler<GetAiConversationQuery, AiConversationDto>
{
    public async Task<AiConversationDto> Handle(GetAiConversationQuery request, CancellationToken cancellationToken)
    {
        var conversation = await repository.GetOwnedAsync(request.ConversationId, cancellationToken) ?? throw new NotFoundException("Conversation not found.");
        var authorizationUnchanged = conversation.AuthorizationFingerprint == await scope.GetAuthorizationFingerprintAsync(cancellationToken);
        var records = await repository.GetMessagesAsync(conversation.Id, cancellationToken);
        var messages = new List<AiConversationMessageDto>(records.Count);
        foreach (var message in records)
        {
            var storedSources = authorizationUnchanged ? await repository.GetSourcesAsync(message.Id, cancellationToken) : [];
            var sources = new List<AiSourceReference>(storedSources.Count);
            foreach (var source in storedSources)
            {
                if (await sourceAuthorization.CanAccessAsync(source, cancellationToken)) sources.Add(source);
            }
            var messageAuthorized = authorizationUnchanged && sources.Count == storedSources.Count;
            JsonElement? result = null;
            if (messageAuthorized && !string.IsNullOrWhiteSpace(message.ResultJson)) result = JsonSerializer.Deserialize<JsonElement>(message.ResultJson);
            messages.Add(new AiConversationMessageDto(message.Id, message.Role, message.Status,
                messageAuthorized ? message.Content : "This message was redacted because your access changed or a source is no longer available.",
                messageAuthorized ? message.ResultType : AiResultType.Unavailable, result,
                messageAuthorized ? message.EvidenceStatus : AiEvidenceStatus.NotFound, message.CreatedAtUtc,
                messageAuthorized ? sources.Select(x => new AiSourceDto(SourceId(x), x.Kind, x.Label, x.EntityType, x.EntityId, x.DocumentId, x.Location, x.Url)).ToArray() : []));
        }
        return new AiConversationDto(conversation.Id, conversation.Title, conversation.ContextType, conversation.ContextId,
            conversation.CreatedAtUtc, conversation.UpdatedAtUtc, messages);
    }

    private static string SourceId(AiSourceReference source)
    {
        if (source.DocumentId is { } documentId)
            return source.DocumentChunkId is { } chunkId ? $"document:{documentId}:{chunkId}" : $"document:{documentId}";
        if (source.EntityType is { Length: > 0 } type && source.EntityId is { } entityId)
            return $"{type.ToLowerInvariant()}:{entityId}";
        return source.Id.ToString();
    }
}
