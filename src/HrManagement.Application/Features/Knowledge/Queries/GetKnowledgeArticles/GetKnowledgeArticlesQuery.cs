using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Knowledge.Queries.GetKnowledgeArticles;

public sealed record GetKnowledgeArticlesQuery(Guid? ProjectId, string? Category, string? Search, bool IncludeDrafts) : IQuery<IReadOnlyList<KnowledgeArticleDto>>;
