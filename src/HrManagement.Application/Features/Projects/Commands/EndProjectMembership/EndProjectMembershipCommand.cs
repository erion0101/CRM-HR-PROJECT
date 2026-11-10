using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Projects.Commands.EndProjectMembership;

public sealed record EndProjectMembershipCommand(Guid ProjectId, Guid MembershipId, DateOnly EffectiveTo, long ExpectedVersion) : ICommand<ProjectDto>;
