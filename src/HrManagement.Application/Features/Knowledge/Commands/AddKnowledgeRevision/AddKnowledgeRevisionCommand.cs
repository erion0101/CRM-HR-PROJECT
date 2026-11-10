using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Knowledge.Commands.AddKnowledgeRevision;

public sealed record AddKnowledgeRevisionCommand(
    Guid ArticleId,
    string Title,
    string Description,
    string Category,
    string Content,
    KnowledgeAudience Audience,
    Guid? DepartmentId,
    int SortOrder,
    string? ChangeSummary,
    long ExpectedVersion) : ICommand<KnowledgeArticleDto>;
