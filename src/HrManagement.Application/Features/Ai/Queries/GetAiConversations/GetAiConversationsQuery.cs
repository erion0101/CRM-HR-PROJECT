using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Queries.GetAiConversations;

public sealed record GetAiConversationsQuery : IQuery<IReadOnlyList<AiConversationDto>>;
