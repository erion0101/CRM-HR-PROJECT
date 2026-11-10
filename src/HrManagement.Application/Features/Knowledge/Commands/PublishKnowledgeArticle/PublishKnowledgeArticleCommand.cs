using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Knowledge.Commands.PublishKnowledgeArticle;

public sealed record PublishKnowledgeArticleCommand(Guid ArticleId, long ExpectedVersion) : ICommand<KnowledgeArticleDto>;
