using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Departments.Commands.AssignDepartmentEmployee;

public sealed record AssignDepartmentEmployeeCommand(
    Guid DepartmentId,
    Guid EmployeeId,
    Guid PositionId,
    DateOnly EffectiveFrom) : ICommand<DepartmentOverviewDto>;
