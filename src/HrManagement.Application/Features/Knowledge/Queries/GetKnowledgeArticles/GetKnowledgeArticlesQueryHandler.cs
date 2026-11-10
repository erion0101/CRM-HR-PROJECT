using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Knowledge.Queries.GetKnowledgeArticles;

public sealed class GetKnowledgeArticlesQueryHandler(IKnowledgeRepository repository)
    : IQueryHandler<GetKnowledgeArticlesQuery, IReadOnlyList<KnowledgeArticleDto>>
{
    public Task<IReadOnlyList<KnowledgeArticleDto>> Handle(GetKnowledgeArticlesQuery request, CancellationToken cancellationToken) =>
        repository.ListVisibleAsync(request.ProjectId, request.Category, request.Search, request.IncludeDrafts, cancellationToken);
}
