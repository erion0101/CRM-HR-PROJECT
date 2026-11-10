using HrManagement.Domain;

namespace HrManagement.Application.Features.Knowledge;

public interface IKnowledgeRepository
{
    void Add(KnowledgeArticle article);
    void Add(KnowledgeArticleRevision revision);
    Task<KnowledgeArticle?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<KnowledgeArticleRevision?> GetRevisionAsync(Guid articleId, int revision, CancellationToken cancellationToken);
    Task<KnowledgeArticleDto?> GetVisibleAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeArticleDto>> ListVisibleAsync(Guid? projectId, string? category, string? search, bool includeDrafts, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> CanManageAsync(KnowledgeArticle article, CancellationToken cancellationToken);
    Task<bool> DepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken);
}
