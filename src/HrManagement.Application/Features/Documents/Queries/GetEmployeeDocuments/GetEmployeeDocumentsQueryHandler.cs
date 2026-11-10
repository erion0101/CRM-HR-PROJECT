using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Employees;

namespace HrManagement.Application.Features.Documents.Queries.GetEmployeeDocuments;

public sealed class GetEmployeeDocumentsQueryHandler(IDocumentRepository documents, IEmployeeRepository employees)
    : IQueryHandler<GetEmployeeDocumentsQuery, IReadOnlyList<DocumentDto>>
{
    public async Task<IReadOnlyList<DocumentDto>> Handle(GetEmployeeDocumentsQuery request, CancellationToken cancellationToken)
    {
        if (!await employees.ExistsAccessibleAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        return (await documents.ListAsync(request.EmployeeId, cancellationToken)).Select(x => x.ToDto()).ToArray();
    }
}
