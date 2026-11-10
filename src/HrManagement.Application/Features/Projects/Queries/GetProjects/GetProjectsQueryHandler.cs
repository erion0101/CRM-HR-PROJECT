using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Projects.Queries.GetProjects;

public sealed class GetProjectsQueryHandler(IProjectRepository repository) : IQueryHandler<GetProjectsQuery, IReadOnlyList<ProjectDto>>
{
    public Task<IReadOnlyList<ProjectDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken) => repository.ListVisibleAsync(cancellationToken);
}
