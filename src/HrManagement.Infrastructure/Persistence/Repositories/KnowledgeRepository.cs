using HrManagement.Application.Common.Authorization;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Knowledge;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Repositories;

internal sealed class KnowledgeRepository(AppDbContext db, ICurrentUserService currentUser) : IKnowledgeRepository
{
    private Guid OrganizationId => currentUser.OrganizationId ?? throw new ForbiddenException();

    public void Add(KnowledgeArticle article) => db.KnowledgeArticles.Add(article);
    public void Add(KnowledgeArticleRevision revision) => db.KnowledgeArticleRevisions.Add(revision);

    public Task<KnowledgeArticle?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.KnowledgeArticles.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.Id == id, cancellationToken);

    public Task<KnowledgeArticleRevision?> GetRevisionAsync(Guid articleId, int revision, CancellationToken cancellationToken) =>
        db.KnowledgeArticleRevisions.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.ArticleId == articleId && x.RevisionNumber == revision, cancellationToken);

    public async Task<KnowledgeArticleDto?> GetVisibleAsync(Guid id, CancellationToken cancellationToken)
    {
        var article = await db.KnowledgeArticles.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.Id == id, cancellationToken);
        if (article is null || !await CanReadAsync(article, includeDrafts: true, cancellationToken)) return null;
        var revisionNumber = await CanManageAsync(article, cancellationToken) ? article.LatestRevision : article.PublishedRevision;
        if (revisionNumber is null) return null;
        var revision = await GetRevisionAsync(article.Id, revisionNumber.Value, cancellationToken);
        return revision is null ? null : Map(article, revision);
    }

    public async Task<IReadOnlyList<KnowledgeArticleDto>> ListVisibleAsync(Guid? projectId, string? category, string? search, bool includeDrafts, CancellationToken cancellationToken)
    {
        var query = db.KnowledgeArticles.AsNoTracking().Where(x => x.OrganizationId == OrganizationId);
        query = projectId.HasValue ? query.Where(x => x.ProjectId == projectId) : query.Where(x => x.ProjectId == null);
        var articles = await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var result = new List<KnowledgeArticleDto>();
        foreach (var article in articles)
        {
            if (!await CanReadAsync(article, includeDrafts, cancellationToken)) continue;
            var revisionNumber = includeDrafts && await CanManageAsync(article, cancellationToken) ? article.LatestRevision : article.PublishedRevision;
            if (revisionNumber is null) continue;
            var revision = await db.KnowledgeArticleRevisions.AsNoTracking().SingleOrDefaultAsync(
                x => x.ArticleId == article.Id && x.RevisionNumber == revisionNumber.Value, cancellationToken);
            if (revision is null) continue;
            if (!string.IsNullOrWhiteSpace(category) && !revision.Category.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                if (!revision.Title.Contains(term, StringComparison.OrdinalIgnoreCase) &&
                    !revision.Description.Contains(term, StringComparison.OrdinalIgnoreCase) &&
                    !revision.Content.Contains(term, StringComparison.OrdinalIgnoreCase)) continue;
            }
            result.Add(Map(article, revision));
        }
        return result;
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        return db.KnowledgeArticles.AnyAsync(x => x.OrganizationId == OrganizationId && x.Slug == normalized && (!excludingId.HasValue || x.Id != excludingId), cancellationToken);
    }

    public Task<bool> DepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken) =>
        db.Departments.AnyAsync(x => x.OrganizationId == OrganizationId && x.Id == departmentId && !x.IsDeleted, cancellationToken);

    public async Task<bool> CanManageAsync(KnowledgeArticle article, CancellationToken cancellationToken)
    {
        if (article.OrganizationId != OrganizationId) return false;
        if (currentUser.IsInRole(Roles.Hr)) return true;
        if (article.ProjectId is null) return false;
        if (currentUser.IsInRole(Roles.ProductManager)) return true;
        if (currentUser.EmployeeId is not { } employeeId) return false;
        return await db.Projects.AnyAsync(x => x.OrganizationId == OrganizationId && x.Id == article.ProjectId && x.LeadEmployeeId == employeeId, cancellationToken);
    }

    private async Task<bool> CanReadAsync(KnowledgeArticle article, bool includeDrafts, CancellationToken cancellationToken)
    {
        if (article.Status == KnowledgeArticleStatus.Archived) return false;
        if (includeDrafts && await CanManageAsync(article, cancellationToken)) return true;
        if (article.PublishedRevision is null) return false;
        if (article.ProjectId.HasValue) return await HasProjectAccessAsync(article.ProjectId.Value, cancellationToken);
        return article.Audience switch
        {
            KnowledgeAudience.AllEmployees => true,
            KnowledgeAudience.Managers => currentUser.IsInRole(Roles.Manager) || currentUser.IsInRole(Roles.Hr) || currentUser.IsInRole(Roles.ProductManager),
            KnowledgeAudience.HrOnly => currentUser.IsInRole(Roles.Hr),
            KnowledgeAudience.Department => article.DepartmentId.HasValue && await IsInDepartmentAsync(article.DepartmentId.Value, cancellationToken),
            _ => false
        };
    }

    private Task<bool> HasProjectAccessAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(Roles.Hr) || currentUser.IsInRole(Roles.ProductManager)) return Task.FromResult(true);
        if (currentUser.EmployeeId is not { } employeeId) return Task.FromResult(false);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return db.Projects.AnyAsync(x => x.OrganizationId == OrganizationId && x.Id == projectId &&
            (x.LeadEmployeeId == employeeId || db.ProjectMemberships.Any(m => m.ProjectId == projectId && m.EmployeeId == employeeId &&
                m.EffectiveFrom <= today && (m.EffectiveTo == null || m.EffectiveTo >= today))), cancellationToken);
    }

    private Task<bool> IsInDepartmentAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is not { } employeeId) return Task.FromResult(false);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return db.PositionAssignments.AnyAsync(x => x.OrganizationId == OrganizationId && x.EmployeeId == employeeId && x.DepartmentId == departmentId &&
            x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today), cancellationToken);
    }

    private static KnowledgeArticleDto Map(KnowledgeArticle article, KnowledgeArticleRevision revision) => new(
        article.Id, article.Slug, revision.Title, revision.Description, revision.Category, revision.Content, revision.ContentFormat,
        article.Audience, article.ProjectId, article.DepartmentId, article.SortOrder, article.Status, revision.RevisionNumber,
        article.LastReviewedAtUtc, article.Version);
}
