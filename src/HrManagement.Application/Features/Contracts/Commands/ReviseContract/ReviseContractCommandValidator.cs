using FluentValidation;

namespace HrManagement.Application.Features.Contracts.Commands.ReviseContract;

public sealed class ReviseContractCommandValidator : AbstractValidator<ReviseContractCommand>
{
    public ReviseContractCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ContractId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}
