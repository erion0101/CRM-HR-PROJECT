namespace HrManagement.Application.Common.Interfaces;

public interface IDocumentStorageService
{
    Task<string> SaveQuarantineAsync(Stream content, CancellationToken cancellationToken);
    Task<string> MoveToPermanentAsync(string quarantineKey, Guid organizationId, Guid documentId, Guid versionId, string extension, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
