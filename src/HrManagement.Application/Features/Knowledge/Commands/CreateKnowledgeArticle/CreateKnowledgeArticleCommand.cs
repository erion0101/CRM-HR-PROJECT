using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Knowledge.Commands.CreateKnowledgeArticle;

public sealed record CreateKnowledgeArticleCommand(
    string Slug,
    string Title,
    string Description,
    string Category,
    string Content,
    KnowledgeAudience Audience,
    Guid? ProjectId,
    Guid? DepartmentId,
    int SortOrder) : ICommand<KnowledgeArticleDto>;
