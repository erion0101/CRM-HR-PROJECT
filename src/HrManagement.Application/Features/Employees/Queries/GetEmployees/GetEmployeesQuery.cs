using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Employees.Queries.GetEmployees;

public sealed record GetEmployeesQuery(string? Search, Guid? DepartmentId, EmploymentStatus? Status, string? Cursor, int Limit = 50) : IQuery<PageResult<EmployeeDto>>;
