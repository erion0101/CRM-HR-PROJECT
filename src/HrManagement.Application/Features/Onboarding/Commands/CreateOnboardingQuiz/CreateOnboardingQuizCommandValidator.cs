using FluentValidation;

namespace HrManagement.Application.Features.Onboarding.Commands.CreateOnboardingQuiz;

public sealed class CreateOnboardingQuizCommandValidator : AbstractValidator<CreateOnboardingQuizCommand>
{
    public CreateOnboardingQuizCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PassingPercentage).InclusiveBetween(1, 100);
        RuleFor(x => x.QuestionCount).InclusiveBetween(1, 100);
    }
}
