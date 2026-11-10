using FluentValidation;

namespace HrManagement.Application.Features.Employees.Commands.AddPositionAssignment;

public sealed class AddPositionAssignmentCommandValidator : AbstractValidator<AddPositionAssignmentCommand>
{
    public AddPositionAssignmentCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.EffectiveTo).GreaterThanOrEqualTo(x => x.EffectiveFrom).When(x => x.EffectiveTo.HasValue);
    }
}
