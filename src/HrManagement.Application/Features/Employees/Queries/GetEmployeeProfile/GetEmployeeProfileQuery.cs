using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Employees.Queries.GetEmployeeProfile;

public sealed record GetEmployeeProfileQuery(Guid EmployeeId) : IQuery<EmployeeProfileDto>;
