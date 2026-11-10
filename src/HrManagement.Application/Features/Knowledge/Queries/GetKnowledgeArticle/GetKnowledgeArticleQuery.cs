using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Knowledge.Queries.GetKnowledgeArticle;

public sealed record GetKnowledgeArticleQuery(Guid Id) : IQuery<KnowledgeArticleDto>;
