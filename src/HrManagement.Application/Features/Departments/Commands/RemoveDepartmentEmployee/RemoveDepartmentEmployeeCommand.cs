using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Departments.Commands.RemoveDepartmentEmployee;

public sealed record RemoveDepartmentEmployeeCommand(
    Guid DepartmentId,
    Guid EmployeeId,
    DateOnly EffectiveOn) : ICommand<DepartmentOverviewDto>;
