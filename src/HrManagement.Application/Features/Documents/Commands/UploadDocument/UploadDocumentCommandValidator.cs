using FluentValidation;
using HrManagement.Application.Common.Interfaces;

namespace HrManagement.Application.Features.Documents.Commands.UploadDocument;

public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator(IDocumentUploadPolicy policy)
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Upload.FileName).NotEmpty().MaximumLength(255).Must(DocumentContentTypes.IsSupported).WithMessage("Only PDF and DOCX files are supported.");
        RuleFor(x => x.Upload.DocumentType).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Upload.Length).InclusiveBetween(1, policy.MaxUploadBytes);
    }
}
