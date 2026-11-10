using HrManagement.Domain;

namespace HrManagement.Application.Features.Documents;

public interface IDocumentRepository
{
    Task<IReadOnlyList<EmployeeDocument>> ListAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<EmployeeDocument?> GetAsync(Guid employeeId, Guid documentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmployeeDocument>> ListAssociatedAsync(DocumentAssociationType associationType, Guid entityId, CancellationToken cancellationToken);
    Task<EmployeeDocument?> GetAssociatedAsync(DocumentAssociationType associationType, Guid entityId, Guid documentId, CancellationToken cancellationToken);
    Task<DocumentVersion> GetCurrentVersionAsync(Guid documentId, int versionNumber, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentFolderDto>> ListFoldersAsync(IReadOnlyCollection<Guid> accessibleEmployeeIds, CancellationToken cancellationToken);
    Task<DocumentFolder?> GetFolderAsync(Guid folderId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentWorkspaceItemDto>> SearchWorkspaceAsync(IReadOnlyCollection<Guid> accessibleEmployeeIds, Guid? folderId, string? search, CancellationToken cancellationToken);
    Task<bool> FolderNameExistsAsync(string name, Guid? employeeId, CancellationToken cancellationToken);
    void Add(EmployeeDocument document);
    void AddFolder(DocumentFolder folder);
    void AddVersion(DocumentVersion version);
    void AddAssociation(DocumentAssociation association);
}
