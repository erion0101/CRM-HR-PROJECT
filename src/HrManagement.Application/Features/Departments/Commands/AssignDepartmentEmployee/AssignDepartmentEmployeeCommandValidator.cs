using FluentValidation;

namespace HrManagement.Application.Features.Departments.Commands.AssignDepartmentEmployee;

public sealed class AssignDepartmentEmployeeCommandValidator : AbstractValidator<AssignDepartmentEmployeeCommand>
{
    public AssignDepartmentEmployeeCommandValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.EffectiveFrom).NotEmpty();
    }
}
