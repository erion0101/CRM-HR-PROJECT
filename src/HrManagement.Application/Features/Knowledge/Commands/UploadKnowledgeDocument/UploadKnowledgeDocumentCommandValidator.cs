using FluentValidation;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Documents;

namespace HrManagement.Application.Features.Knowledge.Commands.UploadKnowledgeDocument;

public sealed class UploadKnowledgeDocumentCommandValidator : AbstractValidator<UploadKnowledgeDocumentCommand>
{
    public UploadKnowledgeDocumentCommandValidator(IDocumentUploadPolicy policy)
    {
        RuleFor(x => x.ArticleId).NotEmpty();
        RuleFor(x => x.Upload.FileName).NotEmpty().MaximumLength(255).Must(DocumentContentTypes.IsSupported).WithMessage("Only PDF and DOCX files are supported.");
        RuleFor(x => x.Upload.DocumentType).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Upload.Length).InclusiveBetween(1, policy.MaxUploadBytes);
    }
}
