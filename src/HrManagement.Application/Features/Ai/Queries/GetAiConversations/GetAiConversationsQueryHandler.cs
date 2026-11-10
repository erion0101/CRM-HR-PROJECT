using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Queries.GetAiConversations;

public sealed class GetAiConversationsQueryHandler(IAiConversationRepository repository) : IQueryHandler<GetAiConversationsQuery, IReadOnlyList<AiConversationDto>>
{
    public async Task<IReadOnlyList<AiConversationDto>> Handle(GetAiConversationsQuery request, CancellationToken cancellationToken) =>
        (await repository.ListOwnedAsync(cancellationToken)).Select(x => new AiConversationDto(x.Id, x.Title, x.ContextType, x.ContextId,
            x.CreatedAtUtc, x.UpdatedAtUtc, [])).ToArray();
}
