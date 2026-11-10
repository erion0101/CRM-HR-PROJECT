using FluentValidation;

namespace HrManagement.Application.Features.Onboarding.Commands.SubmitQuizAttempt;

public sealed class SubmitQuizAttemptCommandValidator : AbstractValidator<SubmitQuizAttemptCommand>
{
    public SubmitQuizAttemptCommandValidator()
    {
        RuleFor(x => x.AttemptId).NotEmpty();
        RuleFor(x => x.Answers).NotEmpty().Must(x => x.Select(a => a.QuestionId).Distinct().Count() == x.Count)
            .WithMessage("Each question may be answered once.");
    }
}
