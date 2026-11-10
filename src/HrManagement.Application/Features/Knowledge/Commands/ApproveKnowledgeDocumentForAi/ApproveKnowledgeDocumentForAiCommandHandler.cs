using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Ai;
using HrManagement.Application.Features.Ai.Commands.ApproveDocumentForAi;
using HrManagement.Application.Features.Documents;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Knowledge.Commands.ApproveKnowledgeDocumentForAi;

public sealed class ApproveKnowledgeDocumentForAiCommandHandler(IKnowledgeRepository knowledge, IDocumentRepository documents,
    IDocumentAiRepository aiDocuments, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    : ICommandHandler<ApproveKnowledgeDocumentForAiCommand, DocumentAiStatusDto>
{
    public async Task<DocumentAiStatusDto> Handle(ApproveKnowledgeDocumentForAiCommand request, CancellationToken cancellationToken)
    {
        var article = await knowledge.GetAsync(request.ArticleId, cancellationToken) ?? throw new NotFoundException("Knowledge article not found.");
        if (!await knowledge.CanManageAsync(article, cancellationToken)) throw new NotFoundException("Knowledge article not found.");
        if (article.Status != KnowledgeArticleStatus.Published)
            throw new ConflictException("Knowledge documents can be approved for AI only while their article is published.");
        var association = article.ProjectId.HasValue ? DocumentAssociationType.ProjectKnowledge : DocumentAssociationType.CompanyKnowledge;
        var document = await documents.GetAssociatedAsync(association, article.Id, request.DocumentId, cancellationToken) ?? throw new NotFoundException("Document not found.");
        var version = await aiDocuments.GetCurrentVersionAsync(document, cancellationToken);
        document.ApproveAiIndexing(request.Classification, currentUser.UserId ?? throw new ForbiddenException(), DateTimeOffset.UtcNow);
        var index = await aiDocuments.GetIndexAsync(version.Id, cancellationToken);
        if (index is null) { index = DocumentAiIndex.Create(document.OrganizationId, document.Id, version.Id, true); aiDocuments.Add(index); }
        else index.Queue(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApproveDocumentForAiCommandHandler.Map(document, version, index);
    }
}
