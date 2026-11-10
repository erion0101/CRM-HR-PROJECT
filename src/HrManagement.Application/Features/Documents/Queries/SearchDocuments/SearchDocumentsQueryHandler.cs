using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Employees;

namespace HrManagement.Application.Features.Documents.Queries.SearchDocuments;

public sealed class SearchDocumentsQueryHandler(IDocumentRepository documents, IEmployeeRepository employees)
    : IQueryHandler<SearchDocumentsQuery, IReadOnlyList<DocumentWorkspaceItemDto>>
{
    public async Task<IReadOnlyList<DocumentWorkspaceItemDto>> Handle(SearchDocumentsQuery request, CancellationToken cancellationToken)
    {
        var accessibleEmployeeIds = await employees.GetAccessibleIdsAsync(cancellationToken);
        if (request.FolderId is { } folderId)
        {
            var folder = await documents.GetFolderAsync(folderId, cancellationToken);
            if (folder is null || (folder.EmployeeId is { } ownerId && !accessibleEmployeeIds.Contains(ownerId)))
                throw new NotFoundException("Document folder not found.");
        }
        return await documents.SearchWorkspaceAsync(accessibleEmployeeIds, request.FolderId, request.Search, cancellationToken);
    }
}
