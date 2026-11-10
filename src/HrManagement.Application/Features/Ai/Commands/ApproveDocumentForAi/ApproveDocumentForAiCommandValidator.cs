using FluentValidation;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Ai.Commands.ApproveDocumentForAi;

public sealed class ApproveDocumentForAiCommandValidator : AbstractValidator<ApproveDocumentForAiCommand>
{
    public ApproveDocumentForAiCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.Classification).Must(x => x is DataClassification.NormalInternal or DataClassification.Private)
            .WithMessage("Only NormalInternal and Private documents can be approved.");
    }
}
