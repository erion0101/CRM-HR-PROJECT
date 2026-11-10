using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommandHandler(IProjectRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await repository.GetAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Project not found.");
        if (!await repository.CanManageAsync(project.Id, cancellationToken)) throw new NotFoundException("Project not found.");
        if (project.Version != request.ExpectedVersion) throw new ConflictException("The project was changed by another user.");
        if (!await repository.EmployeeExistsAsync(request.LeadEmployeeId, cancellationToken)) throw new NotFoundException("Project lead not found.");
        if (request.OnboardingContactEmployeeId is { } contactId && !await repository.EmployeeExistsAsync(contactId, cancellationToken)) throw new NotFoundException("Onboarding contact not found.");
        if (request.DepartmentId is { } departmentId && !await repository.DepartmentExistsAsync(departmentId, cancellationToken)) throw new NotFoundException("Organization unit not found.");
        project.Update(request.Name, request.Purpose, request.Description, request.DepartmentId, request.LeadEmployeeId,
            request.OnboardingContactEmployeeId, request.Status, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetVisibleAsync(project.Id, cancellationToken) ?? throw new InvalidOperationException("Updated project was not available.");
    }
}
