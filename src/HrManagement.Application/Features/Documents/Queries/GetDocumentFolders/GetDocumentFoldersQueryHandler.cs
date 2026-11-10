using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Employees;

namespace HrManagement.Application.Features.Documents.Queries.GetDocumentFolders;

public sealed class GetDocumentFoldersQueryHandler(IDocumentRepository documents, IEmployeeRepository employees)
    : IQueryHandler<GetDocumentFoldersQuery, IReadOnlyList<DocumentFolderDto>>
{
    public async Task<IReadOnlyList<DocumentFolderDto>> Handle(GetDocumentFoldersQuery request, CancellationToken cancellationToken) =>
        await documents.ListFoldersAsync(await employees.GetAccessibleIdsAsync(cancellationToken), cancellationToken);
}
