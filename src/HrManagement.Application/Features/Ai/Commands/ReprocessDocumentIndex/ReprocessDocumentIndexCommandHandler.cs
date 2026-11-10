using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Ai.Commands.ApproveDocumentForAi;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Ai.Commands.ReprocessDocumentIndex;

public sealed class ReprocessDocumentIndexCommandHandler(IDocumentAiRepository repository, IAiAuthorizationService authorization, IUnitOfWork unitOfWork)
    : ICommandHandler<ReprocessDocumentIndexCommand, DocumentAiStatusDto>
{
    public async Task<DocumentAiStatusDto> Handle(ReprocessDocumentIndexCommand request, CancellationToken cancellationToken)
    {
        var document = await repository.GetAccessibleAsync(request.EmployeeId, request.DocumentId, cancellationToken) ?? throw new NotFoundException("Document not found.");
        await authorization.EnsureDocumentApprovalAllowedAsync(request.EmployeeId, document.DataClassification, cancellationToken);
        if (!document.AiIndexingApproved) throw new ConflictException("Document AI indexing has not been approved.");
        var version = await repository.GetCurrentVersionAsync(document, cancellationToken);
        var index = await repository.GetIndexAsync(version.Id, cancellationToken) ?? throw new NotFoundException("Document AI index not found.");
        await repository.DeleteChunksAsync(document.Id, cancellationToken);
        index.Queue(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApproveDocumentForAiCommandHandler.Map(document, version, index);
    }
}
