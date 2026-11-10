using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Employees.Queries.GetEmployeeById;

public sealed record GetEmployeeByIdQuery(Guid Id) : IQuery<EmployeeDto>;
