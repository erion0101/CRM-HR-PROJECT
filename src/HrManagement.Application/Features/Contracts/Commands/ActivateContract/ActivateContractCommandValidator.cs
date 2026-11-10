using FluentValidation;

namespace HrManagement.Application.Features.Contracts.Commands.ActivateContract;

public sealed class ActivateContractCommandValidator : AbstractValidator<ActivateContractCommand>
{
    public ActivateContractCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ContractId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
