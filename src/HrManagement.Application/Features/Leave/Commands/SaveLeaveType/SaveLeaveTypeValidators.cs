using FluentValidation;

namespace HrManagement.Application.Features.Leave.Commands.SaveLeaveType;

public sealed class LeaveTypeInputValidator : AbstractValidator<LeaveTypeInput>
{
    public LeaveTypeInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Color).NotEmpty().Matches("^#[0-9a-fA-F]{6}$").WithMessage("Color must be a hex value such as #2563eb.");
        RuleFor(x => x.DaysPerYear).InclusiveBetween(0.5m, 366).When(x => x.DeductsFromBalance);
        RuleFor(x => x.AccrualMethod).IsInEnum();
        RuleFor(x => x.MaxCarryOverDays).GreaterThanOrEqualTo(0).When(x => x.MaxCarryOverDays.HasValue);
        RuleFor(x => x.CarryOverExpiryMonth).InclusiveBetween(1, 12).When(x => x.CarryOverExpiryMonth.HasValue);
        RuleFor(x => x.CarryOverExpiryDay).InclusiveBetween(1, 31).When(x => x.CarryOverExpiryDay.HasValue);
    }
}

public sealed class CreateLeaveTypeCommandValidator : AbstractValidator<CreateLeaveTypeCommand>
{
    public CreateLeaveTypeCommandValidator() => RuleFor(x => x.Input).NotNull().SetValidator(new LeaveTypeInputValidator());
}

public sealed class UpdateLeaveTypeCommandValidator : AbstractValidator<UpdateLeaveTypeCommand>
{
    public UpdateLeaveTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
        RuleFor(x => x.Input).NotNull().SetValidator(new LeaveTypeInputValidator());
    }
}
