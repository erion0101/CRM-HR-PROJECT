using FluentValidation;

namespace HrManagement.Application.Features.Knowledge.Commands.CreateKnowledgeArticle;

public sealed class CreateKnowledgeArticleCommandValidator : AbstractValidator<CreateKnowledgeArticleCommand>
{
    public CreateKnowledgeArticleCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(160).Matches("^[a-zA-Z0-9-]+$");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(100_000);
        RuleFor(x => x.Audience).IsInEnum();
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
