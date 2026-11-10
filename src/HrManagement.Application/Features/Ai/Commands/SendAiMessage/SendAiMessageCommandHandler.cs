using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Commands.SendAiMessage;

public sealed class SendAiMessageCommandHandler(IAiOrchestrator orchestrator) : ICommandHandler<SendAiMessageCommand, AiMessageResponse>
{
    public Task<AiMessageResponse> Handle(SendAiMessageCommand request, CancellationToken cancellationToken) =>
        orchestrator.SendAsync(request.ConversationId, request.Message, cancellationToken);
}
