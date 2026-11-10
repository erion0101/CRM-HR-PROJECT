using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Projects.Commands.EndProjectMembership;

public sealed class EndProjectMembershipCommandHandler(IProjectRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<EndProjectMembershipCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(EndProjectMembershipCommand request, CancellationToken cancellationToken)
    {
        if (!await repository.CanManageAsync(request.ProjectId, cancellationToken)) throw new NotFoundException("Project not found.");
        var membership = await repository.GetMembershipAsync(request.ProjectId, request.MembershipId, cancellationToken) ?? throw new NotFoundException("Project membership not found.");
        if (membership.Version != request.ExpectedVersion) throw new ConflictException("The membership was changed by another user.");
        membership.End(request.EffectiveTo, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetVisibleAsync(request.ProjectId, cancellationToken) ?? throw new InvalidOperationException("Updated project was not available.");
    }
}
