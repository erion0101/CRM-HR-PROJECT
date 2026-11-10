using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Projects.Queries.GetProject;

public sealed record GetProjectQuery(Guid Id) : IQuery<ProjectDto>;
