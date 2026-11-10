using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandHandler(IProjectRepository repository, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        if (await repository.CodeExistsAsync(request.Code, null, cancellationToken)) throw new ConflictException("A project with this code already exists.");
        if (!await repository.EmployeeExistsAsync(request.LeadEmployeeId, cancellationToken)) throw new NotFoundException("Project lead not found.");
        if (request.OnboardingContactEmployeeId is { } contactId && !await repository.EmployeeExistsAsync(contactId, cancellationToken)) throw new NotFoundException("Onboarding contact not found.");
        if (request.DepartmentId is { } departmentId && !await repository.DepartmentExistsAsync(departmentId, cancellationToken)) throw new NotFoundException("Organization unit not found.");
        var project = Project.Create(organizationId, request.Code, request.Name, request.Purpose, request.Description, request.DepartmentId,
            request.LeadEmployeeId, request.OnboardingContactEmployeeId, request.Status);
        repository.Add(project);
        foreach (var link in request.Links.DistinctBy(x => x.Url, StringComparer.OrdinalIgnoreCase))
            repository.Add(ProjectResourceLink.Create(organizationId, project.Id, link.Label, link.Url, link.ResourceType, link.SortOrder));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetVisibleAsync(project.Id, cancellationToken) ?? throw new InvalidOperationException("Created project was not available.");
    }
}
