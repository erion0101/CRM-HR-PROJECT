using FluentValidation;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Knowledge.Commands.ApproveKnowledgeDocumentForAi;

public sealed class ApproveKnowledgeDocumentForAiCommandValidator : AbstractValidator<ApproveKnowledgeDocumentForAiCommand>
{
    public ApproveKnowledgeDocumentForAiCommandValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty();
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.Classification).Must(x => x is DataClassification.NormalInternal or DataClassification.Private)
            .WithMessage("Only Normal Internal or Private knowledge documents can be indexed.");
    }
}
