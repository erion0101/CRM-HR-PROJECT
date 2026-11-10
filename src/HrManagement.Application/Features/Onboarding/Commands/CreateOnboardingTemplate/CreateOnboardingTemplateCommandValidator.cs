using FluentValidation;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Commands.CreateOnboardingTemplate;

public sealed class CreateOnboardingTemplateCommandValidator : AbstractValidator<CreateOnboardingTemplateCommand>
{
    public CreateOnboardingTemplateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1_000);
        RuleFor(x => x.DurationDays).InclusiveBetween(1, 365);
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.ProjectId).NotEmpty().When(x => x.Kind == OnboardingTemplateKind.Project)
            .WithMessage("Choose the project this onboarding package belongs to.");
        RuleFor(x => x.ProjectId).Null().When(x => x.Kind != OnboardingTemplateKind.Project)
            .WithMessage("Only a project onboarding package can belong to a project.");
        RuleFor(x => x.Steps).NotEmpty().Must(x => x.Count <= 100).WithMessage("A template can have at most 100 steps.");
        RuleFor(x => x.Steps).Must(x => x.Select(step => step.StepOrder).Distinct().Count() == x.Count).WithMessage("Step order must be unique within a template version.");
        RuleForEach(x => x.Steps).SetValidator(new OnboardingTemplateStepInputValidator());
    }
}
