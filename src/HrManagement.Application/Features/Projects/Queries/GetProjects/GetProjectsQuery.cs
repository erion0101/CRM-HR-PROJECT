using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Projects.Queries.GetProjects;

public sealed record GetProjectsQuery : IQuery<IReadOnlyList<ProjectDto>>;
