using FluentValidation;

namespace HrManagement.Application.Features.Onboarding.Commands.AddQuizQuestion;

public sealed class AddQuizQuestionCommandValidator : AbstractValidator<AddQuizQuestionCommand>
{
    public AddQuizQuestionCommandValidator()
    {
        RuleFor(x => x.QuizId).NotEmpty();
        RuleFor(x => x.Question).SetValidator(new QuizQuestionInputValidator());
    }
}
