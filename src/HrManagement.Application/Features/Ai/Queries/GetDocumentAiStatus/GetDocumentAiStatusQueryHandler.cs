using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Ai.Commands.ApproveDocumentForAi;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Ai.Queries.GetDocumentAiStatus;

public sealed class GetDocumentAiStatusQueryHandler(IDocumentAiRepository repository) : IQueryHandler<GetDocumentAiStatusQuery, DocumentAiStatusDto>
{
    public async Task<DocumentAiStatusDto> Handle(GetDocumentAiStatusQuery request, CancellationToken cancellationToken)
    {
        var document = await repository.GetAccessibleAsync(request.EmployeeId, request.DocumentId, cancellationToken) ?? throw new NotFoundException("Document not found.");
        var version = await repository.GetCurrentVersionAsync(document, cancellationToken);
        var index = await repository.GetIndexAsync(version.Id, cancellationToken) ?? DocumentAiIndex.Create(document.OrganizationId, document.Id, version.Id, false);
        return ApproveDocumentForAiCommandHandler.Map(document, version, index);
    }
}
