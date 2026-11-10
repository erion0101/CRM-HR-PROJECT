using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Employees.Commands.SetEmployment;

public sealed record SetEmploymentCommand(Guid EmployeeId, EmploymentStatus Status, DateOnly StartDate, DateOnly? EndDate) : ICommand<EmployeeDto>;
