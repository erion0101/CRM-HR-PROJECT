using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Knowledge.Commands.SubmitKnowledgeForReview;

public sealed record SubmitKnowledgeForReviewCommand(Guid ArticleId, long ExpectedVersion) : ICommand<KnowledgeArticleDto>;
