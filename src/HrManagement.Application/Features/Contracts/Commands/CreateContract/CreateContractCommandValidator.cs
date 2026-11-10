using FluentValidation;

namespace HrManagement.Application.Features.Contracts.Commands.CreateContract;

public sealed class CreateContractCommandValidator : AbstractValidator<CreateContractCommand>
{
    public CreateContractCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ContractNumber).NotEmpty().MaximumLength(80);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}
