using FluentValidation;

namespace HrManagement.Application.Features.Ai.Commands.CreateAiConversation;

public sealed class CreateAiConversationCommandValidator : AbstractValidator<CreateAiConversationCommand>
{
    public CreateAiConversationCommandValidator()
    {
        RuleFor(x => x.Title).MaximumLength(120);
        RuleFor(x => x.ContextType).MaximumLength(40).Must(x => x is null or "Employee" or "Contract" or "Document" or "Organization")
            .WithMessage("Context type is not supported.");
        RuleFor(x => x.ContextId).NotNull().When(x => x.ContextType is "Employee" or "Contract" or "Document");
    }
}
