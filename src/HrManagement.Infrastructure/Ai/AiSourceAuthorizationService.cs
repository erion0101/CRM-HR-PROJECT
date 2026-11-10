using HrManagement.Application.Common.Authorization;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Ai;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Ai;

internal sealed class AiSourceAuthorizationService(
    AppDbContext db,
    IAuthorizedDataScope scope,
    ICurrentUserService currentUser) : IAiSourceAuthorizationService
{
    public async Task<bool> CanAccessAsync(AiSourceReference source, CancellationToken cancellationToken)
    {
        if (source.OrganizationId != scope.OrganizationId) return false;
        if (source.EmployeeId is { } employeeId && !await scope.CanAccessEmployeeAsync(employeeId, cancellationToken)) return false;
        if (source.EntityType == "Project" && source.EntityId is { } projectId && !await CanAccessProjectAsync(projectId, cancellationToken)) return false;
        if (source.EntityType == "KnowledgeArticle" && source.EntityId is { } articleId && !await CanAccessKnowledgeAsync(articleId, cancellationToken)) return false;
        if (source.EntityType == "OnboardingAssignment" && source.EntityId is { } assignmentId &&
            !await db.OnboardingAssignments.AnyAsync(x => x.OrganizationId == scope.OrganizationId && x.Id == assignmentId &&
                (source.EmployeeId == null || x.EmployeeId == source.EmployeeId), cancellationToken)) return false;
        if (source.DocumentId is not { } documentId) return true;

        var document = await db.Documents.Where(x => x.Id == documentId && x.OrganizationId == scope.OrganizationId)
            .Select(x => new { x.Id, x.FolderId, x.Status, x.DataClassification, x.AiIndexingApproved, x.CurrentVersion })
            .SingleOrDefaultAsync(cancellationToken);
        if (document is null || document.Status != DocumentStatus.Clean || !document.AiIndexingApproved ||
            document.DataClassification is DataClassification.Sensitive or DataClassification.HighlyRestricted) return false;
        if (source.EntityType == "KnowledgeArticle" && source.EntityId is { } knowledgeArticleId)
        {
            if (!await db.DocumentAssociations.AnyAsync(x => x.DocumentId == documentId && x.AssociatedEntityId == knowledgeArticleId &&
                    (x.AssociationType == DocumentAssociationType.CompanyKnowledge || x.AssociationType == DocumentAssociationType.ProjectKnowledge), cancellationToken)) return false;
        }
        else if (!await db.DocumentAssociations.AnyAsync(x => x.DocumentId == documentId &&
                     x.AssociationType == DocumentAssociationType.Employee && x.AssociatedEntityId == source.EmployeeId, cancellationToken)) return false;
        if (source.EntityType != "KnowledgeArticle" && document.FolderId is { } folderId)
        {
            var allowedRoles = await db.DocumentFolders.Where(x => x.Id == folderId && x.OrganizationId == scope.OrganizationId)
                .Select(x => x.AllowedRoles).SingleOrDefaultAsync(cancellationToken);
            if (allowedRoles is null || !allowedRoles.Any(currentUser.IsInRole)) return false;
        }
        if (source.DocumentVersionId is not { } versionId) return true;
        return await (from version in db.DocumentVersions
                      join index in db.DocumentAiIndexes on version.Id equals index.DocumentVersionId
                      where version.Id == versionId && version.DocumentId == documentId && version.VersionNumber == document.CurrentVersion &&
                            index.Status == DocumentAiIndexStatus.Processed
                      select version.Id).AnyAsync(cancellationToken);
    }

    private async Task<bool> CanAccessProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(Roles.Hr) || currentUser.IsInRole(Roles.ProductManager))
            return await db.Projects.AnyAsync(x => x.OrganizationId == scope.OrganizationId && x.Id == projectId, cancellationToken);
        if (currentUser.EmployeeId is not { } employeeId) return false;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.Projects.AnyAsync(x => x.OrganizationId == scope.OrganizationId && x.Id == projectId &&
            (x.LeadEmployeeId == employeeId || db.ProjectMemberships.Any(m => m.ProjectId == projectId && m.EmployeeId == employeeId &&
                m.EffectiveFrom <= today && (m.EffectiveTo == null || m.EffectiveTo >= today))), cancellationToken);
    }

    private async Task<bool> CanAccessKnowledgeAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var article = await db.KnowledgeArticles.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == scope.OrganizationId &&
            x.Id == articleId && x.Status == KnowledgeArticleStatus.Published && x.PublishedRevision != null, cancellationToken);
        if (article is null) return false;
        if (article.ProjectId is { } projectId) return await CanAccessProjectAsync(projectId, cancellationToken);
        if (article.Audience == KnowledgeAudience.AllEmployees) return true;
        if (article.Audience == KnowledgeAudience.Managers)
            return currentUser.IsInRole(Roles.Manager) || currentUser.IsInRole(Roles.Hr) || currentUser.IsInRole(Roles.ProductManager);
        if (article.Audience == KnowledgeAudience.HrOnly)
            return currentUser.IsInRole(Roles.Hr);
        if (article.Audience != KnowledgeAudience.Department || article.DepartmentId is not { } departmentId || currentUser.EmployeeId is not { } employeeId) return false;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.PositionAssignments.AnyAsync(x => x.OrganizationId == scope.OrganizationId && x.EmployeeId == employeeId && x.DepartmentId == departmentId &&
            x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today), cancellationToken);
    }
}
