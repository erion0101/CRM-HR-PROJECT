using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Ai.Commands.ApproveDocumentForAi;

public sealed class ApproveDocumentForAiCommandHandler(
    IDocumentAiRepository repository, IAiAuthorizationService authorization, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    : ICommandHandler<ApproveDocumentForAiCommand, DocumentAiStatusDto>
{
    public async Task<DocumentAiStatusDto> Handle(ApproveDocumentForAiCommand request, CancellationToken cancellationToken)
    {
        await authorization.EnsureDocumentApprovalAllowedAsync(request.EmployeeId, request.Classification, cancellationToken);
        var document = await repository.GetAccessibleAsync(request.EmployeeId, request.DocumentId, cancellationToken) ?? throw new NotFoundException("Document not found.");
        var version = await repository.GetCurrentVersionAsync(document, cancellationToken);
        document.ApproveAiIndexing(request.Classification, currentUser.UserId ?? throw new ForbiddenException(), DateTimeOffset.UtcNow);
        var index = await repository.GetIndexAsync(version.Id, cancellationToken);
        if (index is null) { index = DocumentAiIndex.Create(document.OrganizationId, document.Id, version.Id, true); repository.Add(index); }
        else index.Queue(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(document, version, index);
    }

    internal static DocumentAiStatusDto Map(EmployeeDocument document, DocumentVersion version, DocumentAiIndex index) =>
        new(document.Id, version.Id, document.DataClassification, document.AiIndexingApproved, document.AiIndexingApprovedBy,
            document.AiIndexingApprovedAtUtc, index.Status, index.Attempts, index.ErrorCode, index.ProcessedAtUtc);
}
