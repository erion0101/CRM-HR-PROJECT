using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Projects.Queries.GetProject;

public sealed class GetProjectQueryHandler(IProjectRepository repository) : IQueryHandler<GetProjectQuery, ProjectDto>
{
    public async Task<ProjectDto> Handle(GetProjectQuery request, CancellationToken cancellationToken) =>
        await repository.GetVisibleAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Project not found.");
}
