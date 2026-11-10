using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Knowledge.Commands.ArchiveKnowledgeArticle;

public sealed record ArchiveKnowledgeArticleCommand(Guid ArticleId, long ExpectedVersion) : ICommand;
