using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Projects.Commands.UpdateProject;

public sealed record UpdateProjectCommand(
    Guid Id,
    string Name,
    string Purpose,
    string Description,
    Guid? DepartmentId,
    Guid LeadEmployeeId,
    Guid? OnboardingContactEmployeeId,
    ProjectStatus Status,
    long ExpectedVersion) : ICommand<ProjectDto>;
