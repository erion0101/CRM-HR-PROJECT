using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Knowledge.Queries.GetKnowledgeArticle;

public sealed class GetKnowledgeArticleQueryHandler(IKnowledgeRepository repository) : IQueryHandler<GetKnowledgeArticleQuery, KnowledgeArticleDto>
{
    public async Task<KnowledgeArticleDto> Handle(GetKnowledgeArticleQuery request, CancellationToken cancellationToken) =>
        await repository.GetVisibleAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Knowledge article not found.");
}
