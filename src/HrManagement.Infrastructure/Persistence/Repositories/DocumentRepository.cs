using HrManagement.Application.Common.Authorization;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Documents;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Repositories;

internal sealed class DocumentRepository(AppDbContext db, ICurrentUserService currentUser) : IDocumentRepository
{
    public async Task<IReadOnlyList<EmployeeDocument>> ListAsync(Guid employeeId, CancellationToken cancellationToken) =>
        await (from document in db.Documents
               join association in db.DocumentAssociations on document.Id equals association.DocumentId
               where association.AssociationType == DocumentAssociationType.Employee && association.AssociatedEntityId == employeeId
               orderby document.CreatedAtUtc descending
               select document).ToListAsync(cancellationToken);

    public Task<EmployeeDocument?> GetAsync(Guid employeeId, Guid documentId, CancellationToken cancellationToken) =>
        (from document in db.Documents
         join association in db.DocumentAssociations on document.Id equals association.DocumentId
         where document.Id == documentId && association.AssociationType == DocumentAssociationType.Employee && association.AssociatedEntityId == employeeId
         select document).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<EmployeeDocument>> ListAssociatedAsync(DocumentAssociationType associationType, Guid entityId, CancellationToken cancellationToken) =>
        await (from document in db.Documents
               join association in db.DocumentAssociations on document.Id equals association.DocumentId
               where document.OrganizationId == (currentUser.OrganizationId ?? Guid.Empty) && association.AssociationType == associationType && association.AssociatedEntityId == entityId
               orderby document.CreatedAtUtc descending
               select document).ToListAsync(cancellationToken);

    public Task<EmployeeDocument?> GetAssociatedAsync(DocumentAssociationType associationType, Guid entityId, Guid documentId, CancellationToken cancellationToken) =>
        (from document in db.Documents
         join association in db.DocumentAssociations on document.Id equals association.DocumentId
         where document.OrganizationId == (currentUser.OrganizationId ?? Guid.Empty) && document.Id == documentId &&
               association.AssociationType == associationType && association.AssociatedEntityId == entityId
         select document).SingleOrDefaultAsync(cancellationToken);

    public Task<DocumentVersion> GetCurrentVersionAsync(Guid documentId, int versionNumber, CancellationToken cancellationToken) =>
        db.DocumentVersions.SingleAsync(x => x.DocumentId == documentId && x.VersionNumber == versionNumber, cancellationToken);

    public async Task<IReadOnlyList<DocumentFolderDto>> ListFoldersAsync(IReadOnlyCollection<Guid> accessibleEmployeeIds, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var roles = Roles.All.Where(currentUser.IsInRole).ToHashSet(StringComparer.Ordinal);
        var folders = await db.DocumentFolders
            .Where(x => x.OrganizationId == organizationId && (x.Scope == DocumentFolderScope.Global || (x.EmployeeId != null && accessibleEmployeeIds.Contains(x.EmployeeId.Value))))
            .OrderByDescending(x => x.IsSystemFolder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
        var visible = folders.Where(x => x.AllowedRoles.Any(roles.Contains)).ToArray();
        var result = new List<DocumentFolderDto>(visible.Length);
        foreach (var folder in visible)
        {
            var count = await (from document in db.Documents
                               join association in db.DocumentAssociations on document.Id equals association.DocumentId
                               where document.FolderId == folder.Id &&
                                     association.AssociationType == DocumentAssociationType.Employee &&
                                     accessibleEmployeeIds.Contains(association.AssociatedEntityId)
                               select document.Id).Distinct().CountAsync(cancellationToken);
            result.Add(new DocumentFolderDto(folder.Id, folder.Name, folder.Description, folder.IsSystemFolder, folder.Scope, folder.EmployeeId, folder.AllowedRoles, count, folder.Version));
        }
        return result;
    }

    public async Task<DocumentFolder?> GetFolderAsync(Guid folderId, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var folder = await db.DocumentFolders.SingleOrDefaultAsync(x => x.Id == folderId && x.OrganizationId == organizationId, cancellationToken);
        return folder is not null && folder.AllowedRoles.Any(currentUser.IsInRole) ? folder : null;
    }

    public async Task<IReadOnlyList<DocumentWorkspaceItemDto>> SearchWorkspaceAsync(IReadOnlyCollection<Guid> accessibleEmployeeIds, Guid? folderId, string? search, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var query = from document in db.Documents
                    join association in db.DocumentAssociations on document.Id equals association.DocumentId
                    join employee in db.Employees on association.AssociatedEntityId equals employee.Id
                    join version in db.DocumentVersions on new { DocumentId = document.Id, VersionNumber = document.CurrentVersion } equals new { version.DocumentId, version.VersionNumber }
                    where document.OrganizationId == organizationId &&
                          association.AssociationType == DocumentAssociationType.Employee &&
                          accessibleEmployeeIds.Contains(employee.Id)
                    select new { document, employee, version };
        if (folderId is not null) query = query.Where(x => x.document.FolderId == folderId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.document.DisplayName.ToLower().Contains(term) ||
                                     x.employee.FirstName.ToLower().Contains(term) ||
                                     x.employee.LastName.ToLower().Contains(term));
        }
        return await query.OrderByDescending(x => x.document.CreatedAtUtc).Take(200)
            .Select(x => new DocumentWorkspaceItemDto(
                x.document.Id,
                x.employee.Id,
                x.employee.FirstName + " " + x.employee.LastName,
                x.document.FolderId,
                x.document.DisplayName,
                x.document.DocumentType,
                x.version.ContentType,
                x.version.SizeBytes,
                x.document.Status,
                x.document.CurrentVersion,
                x.document.CreatedAtUtc,
                x.document.Version))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> FolderNameExistsAsync(string name, Guid? employeeId, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var normalized = name.Trim().ToLower();
        return db.DocumentFolders.AnyAsync(x => x.OrganizationId == organizationId && x.EmployeeId == employeeId && x.Name.ToLower() == normalized, cancellationToken);
    }

    public void Add(EmployeeDocument document) => db.Documents.Add(document);
    public void AddFolder(DocumentFolder folder) => db.DocumentFolders.Add(folder);
    public void AddVersion(DocumentVersion version) => db.DocumentVersions.Add(version);
    public void AddAssociation(DocumentAssociation association) => db.DocumentAssociations.Add(association);
}
