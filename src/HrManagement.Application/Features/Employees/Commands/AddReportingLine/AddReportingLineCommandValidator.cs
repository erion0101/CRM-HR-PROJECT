using FluentValidation;

namespace HrManagement.Application.Features.Employees.Commands.AddReportingLine;

public sealed class AddReportingLineCommandValidator : AbstractValidator<AddReportingLineCommand>
{
    public AddReportingLineCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.EffectiveTo).GreaterThanOrEqualTo(x => x.EffectiveFrom).When(x => x.EffectiveTo.HasValue);
    }
}
