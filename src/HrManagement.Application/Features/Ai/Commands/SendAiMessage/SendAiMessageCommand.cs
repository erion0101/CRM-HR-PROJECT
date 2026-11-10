using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Commands.SendAiMessage;

public sealed record SendAiMessageCommand(Guid ConversationId, string Message) : ICommand<AiMessageResponse>;
