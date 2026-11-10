using FluentValidation;

namespace HrManagement.Application.Features.Onboarding;

public sealed class OnboardingTemplateStepInputValidator : AbstractValidator<OnboardingTemplateStepInput>
{
    public OnboardingTemplateStepInputValidator()
    {
        RuleFor(x => x.SectionTitle).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Instructions).NotEmpty().MaximumLength(4_000);
        RuleFor(x => x.ExpectedOutcome).NotEmpty().MaximumLength(1_000);
        RuleFor(x => x.StepType).IsInEnum();
        RuleFor(x => x.SectionOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StepOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DueAfterDays).InclusiveBetween(0, 365);
        RuleFor(x => x.KnowledgeArticleId).NotNull().When(x => x.StepType is Domain.OnboardingStepType.ReadArticle or Domain.OnboardingStepType.PolicyAcknowledgment);
        RuleFor(x => x.DocumentId).NotNull().When(x => x.StepType == Domain.OnboardingStepType.ReadDocument);
        RuleFor(x => x.ExternalUrl).NotEmpty().When(x => x.StepType == Domain.OnboardingStepType.ExternalResource);
        RuleFor(x => x.ExternalUrl).Must(value => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            .When(x => !string.IsNullOrWhiteSpace(x.ExternalUrl));
    }
}
