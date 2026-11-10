using FluentValidation;

namespace HrManagement.Application.Features.Compensation.Commands.AddCompensation;

public sealed class AddCompensationCommandValidator : AbstractValidator<AddCompensationCommand>
{
    public AddCompensationCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Frequency).NotEmpty().MaximumLength(30);
        RuleFor(x => x.EffectiveTo).GreaterThanOrEqualTo(x => x.EffectiveFrom).When(x => x.EffectiveTo.HasValue);
    }
}
