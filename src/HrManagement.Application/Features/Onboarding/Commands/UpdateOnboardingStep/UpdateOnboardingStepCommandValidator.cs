using FluentValidation;

namespace HrManagement.Application.Features.Onboarding.Commands.UpdateOnboardingStep;

public sealed class UpdateOnboardingStepCommandValidator : AbstractValidator<UpdateOnboardingStepCommand>
{
    public UpdateOnboardingStepCommandValidator()
    {
        RuleFor(x => x.AssignmentId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Status).NotEqual(Domain.OnboardingStepStatus.PendingReview).WithMessage("Pending review is assigned automatically to reviewed milestones.");
        RuleFor(x => x.CompletionNote).MaximumLength(2_000);
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
