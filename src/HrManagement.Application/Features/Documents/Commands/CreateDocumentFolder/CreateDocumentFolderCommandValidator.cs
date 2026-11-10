using FluentValidation;
using HrManagement.Application.Common.Authorization;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Documents.Commands.CreateDocumentFolder;

public sealed class CreateDocumentFolderCommandValidator : AbstractValidator<CreateDocumentFolderCommand>
{
    public CreateDocumentFolderCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.AllowedRoles).NotEmpty();
        RuleForEach(x => x.AllowedRoles).Must(Roles.All.Contains).WithMessage("An allowed role is invalid.");
        RuleFor(x => x.EmployeeId).NotEmpty().When(x => x.Scope == DocumentFolderScope.Employee);
        RuleFor(x => x.EmployeeId).Empty().When(x => x.Scope == DocumentFolderScope.Global);
    }
}
