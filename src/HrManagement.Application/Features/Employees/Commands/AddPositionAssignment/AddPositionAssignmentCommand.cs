using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Employees.Commands.AddPositionAssignment;

public sealed record AddPositionAssignmentCommand(Guid EmployeeId, Guid PositionId, Guid DepartmentId, DateOnly EffectiveFrom, DateOnly? EffectiveTo) : ICommand<EmployeeDto>;
