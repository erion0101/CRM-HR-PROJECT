using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;
using HrManagement.Application.Features.Documents;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Knowledge.Queries.DownloadKnowledgeDocument;

public sealed class DownloadKnowledgeDocumentQueryHandler(IKnowledgeRepository knowledge, IDocumentRepository documents,
    IDocumentStorageService storage, IAuditWriter audit) : IQueryHandler<DownloadKnowledgeDocumentQuery, DocumentDownload>
{
    public async Task<DocumentDownload> Handle(DownloadKnowledgeDocumentQuery request, CancellationToken cancellationToken)
    {
        var article = await knowledge.GetAsync(request.ArticleId, cancellationToken) ?? throw new NotFoundException("Knowledge article not found.");
        if (await knowledge.GetVisibleAsync(article.Id, cancellationToken) is null) throw new NotFoundException("Knowledge article not found.");
        var type = article.ProjectId.HasValue ? DocumentAssociationType.ProjectKnowledge : DocumentAssociationType.CompanyKnowledge;
        var document = await documents.GetAssociatedAsync(type, article.Id, request.DocumentId, cancellationToken) ?? throw new NotFoundException("Document not found.");
        if (document.Status != DocumentStatus.Clean) throw new ConflictException("Document is not available until scanning completes.");
        var version = await documents.GetCurrentVersionAsync(document.Id, document.CurrentVersion, cancellationToken);
        var stream = await storage.OpenReadAsync(version.StorageKey, cancellationToken);
        await audit.RecordAccessAsync("DownloadKnowledge", nameof(EmployeeDocument), document.Id.ToString(), cancellationToken);
        return new DocumentDownload(version.OriginalFileName, version.ContentType, version.SizeBytes, stream);
    }
}
