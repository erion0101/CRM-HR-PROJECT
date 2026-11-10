using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Documents;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Knowledge.Queries.GetKnowledgeDocuments;

public sealed class GetKnowledgeDocumentsQueryHandler(IKnowledgeRepository knowledge, IDocumentRepository documents)
    : IQueryHandler<GetKnowledgeDocumentsQuery, IReadOnlyList<DocumentDto>>
{
    public async Task<IReadOnlyList<DocumentDto>> Handle(GetKnowledgeDocumentsQuery request, CancellationToken cancellationToken)
    {
        var article = await knowledge.GetAsync(request.ArticleId, cancellationToken) ?? throw new NotFoundException("Knowledge article not found.");
        if (await knowledge.GetVisibleAsync(article.Id, cancellationToken) is null) throw new NotFoundException("Knowledge article not found.");
        var type = article.ProjectId.HasValue ? DocumentAssociationType.ProjectKnowledge : DocumentAssociationType.CompanyKnowledge;
        return (await documents.ListAssociatedAsync(type, article.Id, cancellationToken)).Select(x => x.ToDto()).ToArray();
    }
}
