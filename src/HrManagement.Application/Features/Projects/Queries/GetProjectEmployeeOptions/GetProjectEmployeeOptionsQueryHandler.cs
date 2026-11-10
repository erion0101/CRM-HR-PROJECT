using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Projects.Queries.GetProjectEmployeeOptions;

public sealed class GetProjectEmployeeOptionsQueryHandler(IProjectRepository repository)
    : IQueryHandler<GetProjectEmployeeOptionsQuery, IReadOnlyList<ProjectEmployeeOptionDto>>
{
    public Task<IReadOnlyList<ProjectEmployeeOptionDto>> Handle(GetProjectEmployeeOptionsQuery request, CancellationToken cancellationToken) =>
        repository.ListEmployeeOptionsAsync(cancellationToken);
}
