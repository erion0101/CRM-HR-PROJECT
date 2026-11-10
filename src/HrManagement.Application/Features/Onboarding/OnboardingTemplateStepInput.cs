using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding;

public sealed record OnboardingTemplateStepInput(
    string SectionTitle,
    int SectionOrder,
    string Title,
    string Instructions,
    string ExpectedOutcome,
    OnboardingStepType StepType,
    int StepOrder,
    int DueAfterDays,
    bool IsRequired,
    Guid? KnowledgeArticleId,
    Guid? DocumentId,
    string? ExternalUrl,
    int? DependsOnStepOrder);
