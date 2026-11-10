using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Employees.Commands.CreateEmployee;

public sealed record CreateEmployeeCommand(string EmployeeNumber, string FirstName, string LastName, string WorkEmail, string? Phone, DateOnly? DateOfBirth = null) : ICommand<EmployeeDto>;
