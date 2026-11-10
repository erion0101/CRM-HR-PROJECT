using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Projects.Commands.AddProjectMembership;

public sealed record AddProjectMembershipCommand(
    Guid ProjectId,
    Guid EmployeeId,
    string ProjectRole,
    string Responsibilities,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo) : ICommand<ProjectDto>;
