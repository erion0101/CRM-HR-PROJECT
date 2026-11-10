using FluentValidation;

namespace HrManagement.Application.Features.Knowledge.Commands.AddKnowledgeRevision;

public sealed class AddKnowledgeRevisionCommandValidator : AbstractValidator<AddKnowledgeRevisionCommand>
{
    public AddKnowledgeRevisionCommandValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(100_000);
        RuleFor(x => x.ChangeSummary).MaximumLength(500);
        RuleFor(x => x.Audience).IsInEnum();
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
