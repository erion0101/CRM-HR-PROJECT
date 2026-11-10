using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding;

public sealed record OnboardingTemplateDto(
    Guid Id,
    string Name,
    string Description,
    Guid? DepartmentId,
    Guid? PositionId,
    OnboardingTemplateKind Kind,
    Guid? ProjectId,
    bool AutoAssignToNewEmployees,
    int DurationDays,
    OnboardingTemplateStatus Status,
    int CurrentVersion,
    int? PublishedVersion,
    IReadOnlyList<OnboardingTemplateStepDto> Steps,
    long Version);
