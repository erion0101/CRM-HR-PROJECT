using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;
using HrManagement.Application.Features.Employees;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Documents.Queries.DownloadDocument;

public sealed class DownloadDocumentQueryHandler(IDocumentRepository documents, IEmployeeRepository employees, IDocumentStorageService storage, IAuditWriter audit)
    : IQueryHandler<DownloadDocumentQuery, DocumentDownload>
{
    public async Task<DocumentDownload> Handle(DownloadDocumentQuery request, CancellationToken cancellationToken)
    {
        if (!await employees.ExistsAccessibleAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        var document = await documents.GetAsync(request.EmployeeId, request.DocumentId, cancellationToken) ?? throw new NotFoundException("Document not found.");
        if (document.Status != DocumentStatus.Clean) throw new ConflictException("Document is not available until scanning completes.");
        var version = await documents.GetCurrentVersionAsync(document.Id, document.CurrentVersion, cancellationToken);
        var stream = await storage.OpenReadAsync(version.StorageKey, cancellationToken);
        await audit.RecordAccessAsync("Download", nameof(EmployeeDocument), document.Id.ToString(), cancellationToken);
        return new DocumentDownload(version.OriginalFileName, version.ContentType, version.SizeBytes, stream);
    }
}
