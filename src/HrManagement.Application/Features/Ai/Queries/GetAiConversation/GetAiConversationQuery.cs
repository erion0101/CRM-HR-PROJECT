using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Queries.GetAiConversation;

public sealed record GetAiConversationQuery(Guid ConversationId) : IQuery<AiConversationDto>;
