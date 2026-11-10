using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Projects.Queries.GetProjectEmployeeOptions;

public sealed record GetProjectEmployeeOptionsQuery : IQuery<IReadOnlyList<ProjectEmployeeOptionDto>>;
