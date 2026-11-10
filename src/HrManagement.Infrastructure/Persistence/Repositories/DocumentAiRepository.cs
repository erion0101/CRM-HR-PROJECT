using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Ai;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Repositories;

internal sealed class DocumentAiRepository(AppDbContext db, IAuthorizedDataScope scope) : IDocumentAiRepository
{
    public async Task<EmployeeDocument?> GetAccessibleAsync(Guid employeeId, Guid documentId, CancellationToken cancellationToken)
    {
        if (!await scope.CanAccessEmployeeAsync(employeeId, cancellationToken)) return null;
        return await (from document in db.Documents
                      join association in db.DocumentAssociations on document.Id equals association.DocumentId
                      where document.OrganizationId == scope.OrganizationId && document.Id == documentId &&
                            association.AssociationType == DocumentAssociationType.Employee && association.AssociatedEntityId == employeeId
                      select document).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<DocumentVersion> GetCurrentVersionAsync(EmployeeDocument document, CancellationToken cancellationToken) =>
        db.DocumentVersions.SingleAsync(x => x.DocumentId == document.Id && x.VersionNumber == document.CurrentVersion, cancellationToken);

    public Task<DocumentAiIndex?> GetIndexAsync(Guid documentVersionId, CancellationToken cancellationToken) =>
        db.DocumentAiIndexes.SingleOrDefaultAsync(x => x.OrganizationId == scope.OrganizationId && x.DocumentVersionId == documentVersionId, cancellationToken);

    public void Add(DocumentAiIndex index) => db.DocumentAiIndexes.Add(index);

    public Task DeleteChunksAsync(Guid documentId, CancellationToken cancellationToken) =>
        db.DocumentChunks.Where(x => x.OrganizationId == scope.OrganizationId && x.DocumentId == documentId).ExecuteDeleteAsync(cancellationToken);
}
