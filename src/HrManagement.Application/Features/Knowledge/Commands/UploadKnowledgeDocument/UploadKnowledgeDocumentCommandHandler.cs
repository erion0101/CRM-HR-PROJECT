using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Documents;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Knowledge.Commands.UploadKnowledgeDocument;

public sealed class UploadKnowledgeDocumentCommandHandler(IKnowledgeRepository knowledge, IDocumentRepository documents,
    IDocumentStorageService storage, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    : ICommandHandler<UploadKnowledgeDocumentCommand, DocumentDto>
{
    public async Task<DocumentDto> Handle(UploadKnowledgeDocumentCommand request, CancellationToken cancellationToken)
    {
        var article = await knowledge.GetAsync(request.ArticleId, cancellationToken) ?? throw new NotFoundException("Knowledge article not found.");
        if (!await knowledge.CanManageAsync(article, cancellationToken)) throw new NotFoundException("Knowledge article not found.");
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var document = EmployeeDocument.Create(organizationId, request.Upload.FileName, request.Upload.DocumentType);
        var storageKey = await storage.SaveQuarantineAsync(request.Upload.Content, cancellationToken);
        var version = DocumentVersion.Create(organizationId, document.Id, 1, request.Upload.FileName, storageKey,
            DocumentContentTypes.FromFileName(request.Upload.FileName), request.Upload.Length);
        documents.Add(document);
        documents.AddVersion(version);
        documents.AddAssociation(DocumentAssociation.Create(organizationId, document.Id,
            article.ProjectId.HasValue ? DocumentAssociationType.ProjectKnowledge : DocumentAssociationType.CompanyKnowledge, article.Id));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return document.ToDto();
    }
}
