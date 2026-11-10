using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Projects.Commands.CreateProject;

public sealed record CreateProjectCommand(
    string Code,
    string Name,
    string Purpose,
    string Description,
    Guid? DepartmentId,
    Guid LeadEmployeeId,
    Guid? OnboardingContactEmployeeId,
    ProjectStatus Status,
    IReadOnlyList<ProjectLinkInput> Links) : ICommand<ProjectDto>;
