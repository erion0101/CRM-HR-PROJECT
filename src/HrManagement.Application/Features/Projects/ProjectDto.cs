using HrManagement.Domain;

namespace HrManagement.Application.Features.Projects;

public sealed record ProjectDto(
    Guid Id,
    string Code,
    string Name,
    string Purpose,
    string Description,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid LeadEmployeeId,
    string LeadEmployeeName,
    Guid? OnboardingContactEmployeeId,
    string? OnboardingContactEmployeeName,
    ProjectStatus Status,
    bool CanManage,
    IReadOnlyList<ProjectMemberDto> Members,
    IReadOnlyList<ProjectResourceLinkDto> Links,
    long Version);
