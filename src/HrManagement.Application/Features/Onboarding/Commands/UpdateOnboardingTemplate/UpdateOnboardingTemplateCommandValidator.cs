using FluentValidation;

namespace HrManagement.Application.Features.Onboarding.Commands.UpdateOnboardingTemplate;

public sealed class UpdateOnboardingTemplateCommandValidator : AbstractValidator<UpdateOnboardingTemplateCommand>
{
    public UpdateOnboardingTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1_000);
        RuleFor(x => x.DurationDays).InclusiveBetween(1, 365);
        RuleFor(x => x.Steps).NotEmpty().Must(x => x.Count <= 100);
        RuleFor(x => x.Steps).Must(x => x.Select(step => step.StepOrder).Distinct().Count() == x.Count)
            .WithMessage("Step order must be unique within a template version.");
        RuleForEach(x => x.Steps).SetValidator(new OnboardingTemplateStepInputValidator());
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
