namespace HrManagement.Application.Features.Onboarding;

public sealed record OnboardingDashboardDto(
    Guid EmployeeId,
    string EmployeeName,
    string? Position,
    string? Department,
    IReadOnlyList<HrManagement.Application.Features.Projects.ProjectDto> Projects,
    IReadOnlyList<OnboardingAssignmentDto> Journeys);
