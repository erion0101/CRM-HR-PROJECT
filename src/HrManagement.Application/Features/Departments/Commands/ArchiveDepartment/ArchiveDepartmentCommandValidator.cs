using FluentValidation;

namespace HrManagement.Application.Features.Departments.Commands.ArchiveDepartment;

public sealed class ArchiveDepartmentCommandValidator : AbstractValidator<ArchiveDepartmentCommand>
{
    public ArchiveDepartmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
