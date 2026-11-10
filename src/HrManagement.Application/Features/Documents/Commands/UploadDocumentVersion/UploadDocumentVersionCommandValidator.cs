using FluentValidation;
using HrManagement.Application.Common.Interfaces;

namespace HrManagement.Application.Features.Documents.Commands.UploadDocumentVersion;

public sealed class UploadDocumentVersionCommandValidator : AbstractValidator<UploadDocumentVersionCommand>
{
    public UploadDocumentVersionCommandValidator(IDocumentUploadPolicy policy)
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
        RuleFor(x => x.Upload.FileName).NotEmpty().MaximumLength(255).Must(DocumentContentTypes.IsSupported).WithMessage("Only PDF and DOCX files are supported.");
        RuleFor(x => x.Upload.DocumentType).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Upload.Length).InclusiveBetween(1, policy.MaxUploadBytes);
    }
}
