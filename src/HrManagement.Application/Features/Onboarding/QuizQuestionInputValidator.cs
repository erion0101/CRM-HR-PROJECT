using FluentValidation;

namespace HrManagement.Application.Features.Onboarding;

public sealed class QuizQuestionInputValidator : AbstractValidator<QuizQuestionInput>
{
    public QuizQuestionInputValidator()
    {
        RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(2_000);
        RuleFor(x => x.Explanation).NotEmpty().MaximumLength(4_000);
        RuleFor(x => x.Topic).NotEmpty().MaximumLength(160);
        RuleFor(x => x.SourceLabel).NotEmpty().MaximumLength(240);
        RuleFor(x => x).Must(x => x.SourceDocumentId.HasValue && x.SourceDocumentVersionId.HasValue || x.SourceKnowledgeRevisionId.HasValue)
            .WithMessage("A question must reference a published source version.");
        RuleFor(x => x.Options).Must(x => x.Count == 4).WithMessage("Multiple-choice questions require exactly four answer options.");
        RuleFor(x => x.Options).Must(x => x.Count(option => option.IsCorrect) == 1).WithMessage("Exactly one answer option must be correct.");
        RuleFor(x => x.Options).Must(x => x.Select(option => option.SortOrder).Distinct().Count() == x.Count).WithMessage("Answer option order must be unique.");
        RuleForEach(x => x.Options).ChildRules(option => option.RuleFor(x => x.Text).NotEmpty().MaximumLength(1_000));
    }
}
