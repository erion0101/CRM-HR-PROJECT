using FluentValidation;

namespace HrManagement.Application.Features.Departments.Commands.RemoveDepartmentEmployee;

public sealed class RemoveDepartmentEmployeeCommandValidator : AbstractValidator<RemoveDepartmentEmployeeCommand>
{
    public RemoveDepartmentEmployeeCommandValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.EffectiveOn).NotEmpty();
    }
}
