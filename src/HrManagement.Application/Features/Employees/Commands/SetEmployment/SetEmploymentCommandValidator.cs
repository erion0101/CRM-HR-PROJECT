using FluentValidation;

namespace HrManagement.Application.Features.Employees.Commands.SetEmployment;

public sealed class SetEmploymentCommandValidator : AbstractValidator<SetEmploymentCommand>
{
    public SetEmploymentCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
    }
}
