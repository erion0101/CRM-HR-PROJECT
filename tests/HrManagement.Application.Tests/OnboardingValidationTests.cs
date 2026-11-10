using HrManagement.Application.Features.Onboarding;
using HrManagement.Application.Features.Onboarding.Commands.CreateOnboardingTemplate;
using HrManagement.Domain;
using Xunit;

namespace HrManagement.Application.Tests;

public sealed class OnboardingValidationTests
{
    [Fact]
    public void Template_validator_rejects_duplicate_step_order()
    {
        var step = new OnboardingTemplateStepInput("First week", 0, "Read", "Read this", "Understood",
            OnboardingStepType.PracticalTask, 1, 1, true, null, null, null, null);
        var command = new CreateOnboardingTemplateCommand("Template", "Description", null, null, 30, [step, step]);

        var result = new CreateOnboardingTemplateCommandValidator().Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateOnboardingTemplateCommand.Steps));
    }

    [Fact]
    public void Article_step_requires_a_knowledge_article()
    {
        var input = new OnboardingTemplateStepInput("Day one", 0, "Read overview", "Read it", "Understood",
            OnboardingStepType.ReadArticle, 0, 0, true, null, null, null, null);

        var result = new OnboardingTemplateStepInputValidator().Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(OnboardingTemplateStepInput.KnowledgeArticleId));
    }
}
