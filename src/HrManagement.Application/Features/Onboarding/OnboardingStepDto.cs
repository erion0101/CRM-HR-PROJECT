using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding;

public sealed record OnboardingStepDto(
    Guid Id,
    string SectionTitle,
    int SectionOrder,
    string Title,
    string Instructions,
    string ExpectedOutcome,
    OnboardingStepType StepType,
    int StepOrder,
    bool IsRequired,
    DateOnly DueDate,
    bool IsOverdue,
    Guid? KnowledgeArticleId,
    Guid? DocumentId,
    string? ExternalUrl,
    OnboardingStepStatus Status,
    string? CompletionNote,
    DateTimeOffset? CompletedAtUtc,
    string? ReviewNote,
    long Version);
