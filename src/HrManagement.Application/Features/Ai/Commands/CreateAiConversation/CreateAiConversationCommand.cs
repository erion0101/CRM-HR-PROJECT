using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Commands.CreateAiConversation;

public sealed record CreateAiConversationCommand(string? Title, string? ContextType, Guid? ContextId) : ICommand<AiConversationDto>;
