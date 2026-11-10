using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Employees.Commands.UpdateEmployee;

public sealed record UpdateEmployeeCommand(Guid Id, string FirstName, string LastName, string WorkEmail, string? Phone, DateOnly? DateOfBirth, long ExpectedVersion) : ICommand<EmployeeDto>;
