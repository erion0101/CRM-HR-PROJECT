using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding;

public sealed record OnboardingAssignmentDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid TemplateId,
    string TemplateName,
    int TemplateVersion,
    Guid? ProjectId,
    string? ProjectName,
    Guid? ProjectTemplateId,
    string? ProjectTemplateName,
    Guid? ManagerEmployeeId,
    string? ManagerName,
    Guid? MentorEmployeeId,
    string? MentorName,
    DateOnly StartDate,
    OnboardingAssignmentStatus Status,
    int ProgressPercent,
    DateTimeOffset? CompletedAtUtc,
    IReadOnlyList<OnboardingStepDto> Steps,
    OnboardingAssessmentDto Assessment,
    long Version);
