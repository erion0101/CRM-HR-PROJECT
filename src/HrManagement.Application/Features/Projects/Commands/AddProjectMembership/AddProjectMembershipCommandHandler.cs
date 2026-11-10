using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Projects.Commands.AddProjectMembership;

public sealed class AddProjectMembershipCommandHandler(IProjectRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<AddProjectMembershipCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(AddProjectMembershipCommand request, CancellationToken cancellationToken)
    {
        var project = await repository.GetAsync(request.ProjectId, cancellationToken) ?? throw new NotFoundException("Project not found.");
        if (!await repository.CanManageAsync(project.Id, cancellationToken)) throw new NotFoundException("Project not found.");
        if (!await repository.EmployeeExistsAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        if (await repository.HasOverlappingMembershipAsync(project.Id, request.EmployeeId, request.EffectiveFrom, request.EffectiveTo, cancellationToken))
            throw new ConflictException("The employee already has an overlapping project membership.");
        repository.Add(ProjectMembership.Create(project.OrganizationId, project.Id, request.EmployeeId, request.ProjectRole,
            request.Responsibilities, request.EffectiveFrom, request.EffectiveTo));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetVisibleAsync(project.Id, cancellationToken) ?? throw new InvalidOperationException("Updated project was not available.");
    }
}
