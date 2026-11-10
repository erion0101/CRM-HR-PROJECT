using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Ai.Commands.RevokeDocumentAiApproval;

public sealed class RevokeDocumentAiApprovalCommandHandler(IDocumentAiRepository repository, IAiAuthorizationService authorization, IUnitOfWork unitOfWork)
    : ICommandHandler<RevokeDocumentAiApprovalCommand>
{
    public async Task Handle(RevokeDocumentAiApprovalCommand request, CancellationToken cancellationToken)
    {
        await authorization.EnsureDocumentApprovalAllowedAsync(request.EmployeeId, DataClassification.NormalInternal, cancellationToken);
        var document = await repository.GetAccessibleAsync(request.EmployeeId, request.DocumentId, cancellationToken) ?? throw new NotFoundException("Document not found.");
        var version = await repository.GetCurrentVersionAsync(document, cancellationToken);
        document.RevokeAiIndexing(DateTimeOffset.UtcNow);
        var index = await repository.GetIndexAsync(version.Id, cancellationToken);
        index?.MarkNotEligible(DateTimeOffset.UtcNow);
        await repository.DeleteChunksAsync(document.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
