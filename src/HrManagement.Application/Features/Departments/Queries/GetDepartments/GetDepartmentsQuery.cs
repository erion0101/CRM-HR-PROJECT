using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.Departments.Queries.GetDepartments;

public sealed record GetDepartmentsQuery(string? Cursor, int Limit = 50) : IQuery<PageResult<DepartmentDto>>;
