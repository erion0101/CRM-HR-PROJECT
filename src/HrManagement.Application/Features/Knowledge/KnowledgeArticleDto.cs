using HrManagement.Domain;

namespace HrManagement.Application.Features.Knowledge;

public sealed record KnowledgeArticleDto(
    Guid Id,
    string Slug,
    string Title,
    string Description,
    string Category,
    string Content,
    KnowledgeContentFormat ContentFormat,
    KnowledgeAudience Audience,
    Guid? ProjectId,
    Guid? DepartmentId,
    int SortOrder,
    KnowledgeArticleStatus Status,
    int Revision,
    DateTimeOffset? LastReviewedAtUtc,
    long Version);
