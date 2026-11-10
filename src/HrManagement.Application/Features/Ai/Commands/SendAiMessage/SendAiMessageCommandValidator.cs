using FluentValidation;

namespace HrManagement.Application.Features.Ai.Commands.SendAiMessage;

public sealed class SendAiMessageCommandValidator : AbstractValidator<SendAiMessageCommand>
{
    public SendAiMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4_000);
    }
}
