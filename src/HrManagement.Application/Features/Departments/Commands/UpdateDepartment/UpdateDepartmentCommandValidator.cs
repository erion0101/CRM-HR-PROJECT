using FluentValidation;

namespace HrManagement.Application.Features.Departments.Commands.UpdateDepartment;

public sealed class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Code).MaximumLength(30);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.ParentDepartmentId).NotEqual(x => x.Id).When(x => x.ParentDepartmentId is not null)
            .WithMessage("An organization unit cannot be its own parent.");
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
