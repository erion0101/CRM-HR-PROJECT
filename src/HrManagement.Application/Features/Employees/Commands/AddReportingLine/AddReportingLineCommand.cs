using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Employees.Commands.AddReportingLine;

public sealed record AddReportingLineCommand(Guid EmployeeId, DateOnly EffectiveFrom, DateOnly? EffectiveTo) : ICommand<EmployeeDto>;
