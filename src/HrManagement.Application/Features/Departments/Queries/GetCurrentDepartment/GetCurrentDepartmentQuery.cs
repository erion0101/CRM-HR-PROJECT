using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Departments.Queries.GetCurrentDepartment;

public sealed record GetCurrentDepartmentQuery(Guid? DepartmentId = null) : IQuery<DepartmentOverviewDto>;
