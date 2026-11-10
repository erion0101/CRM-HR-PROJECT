using FluentValidation;

namespace HrManagement.Application.Features.Onboarding.Commands.UpdateQuizQuestion;

public sealed class UpdateQuizQuestionCommandValidator : AbstractValidator<UpdateQuizQuestionCommand>
{
    public UpdateQuizQuestionCommandValidator() => RuleFor(x => x.Question).SetValidator(new QuizQuestionInputValidator());
}
