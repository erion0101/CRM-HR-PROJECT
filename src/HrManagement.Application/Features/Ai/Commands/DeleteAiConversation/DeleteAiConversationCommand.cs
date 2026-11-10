using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Commands.DeleteAiConversation;

public sealed record DeleteAiConversationCommand(Guid ConversationId) : ICommand;
