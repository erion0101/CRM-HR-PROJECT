using FluentValidation;

namespace HrManagement.Application.Features.Employees.Commands.ArchiveEmployee;

public sealed class ArchiveEmployeeCommandValidator : AbstractValidator<ArchiveEmployeeCommand>
{
    public ArchiveEmployeeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
