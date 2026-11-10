using HrManagement.Application.Common.Interfaces;
using HrManagement.Infrastructure.Files.Options;
using Microsoft.Extensions.Options;

namespace HrManagement.Infrastructure.Files;

public sealed class LocalDocumentStorage(IOptions<StorageOptions> options) : IDocumentStorageService
{
    private readonly string _root = Path.GetFullPath(options.Value.RootPath);

    public async Task<string> SaveQuarantineAsync(Stream content, CancellationToken cancellationToken)
    {
        var key = Path.Combine("quarantine", $"{Guid.CreateVersion7():N}.upload");
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81_920, FileOptions.Asynchronous | FileOptions.WriteThrough);
        await content.CopyToAsync(output, cancellationToken);
        return key.Replace(Path.DirectorySeparatorChar, '/');
    }

    public Task<string> MoveToPermanentAsync(string quarantineKey, Guid organizationId, Guid documentId, Guid versionId, string extension, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedExtension = extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) ? ".pdf" : ".docx";
        var key = Path.Combine("documents", organizationId.ToString("N"), documentId.ToString("N"), versionId.ToString("N"), $"original{normalizedExtension}");
        var source = Resolve(quarantineKey);
        var destination = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Move(source, destination, overwrite: false);
        return Task.FromResult(key.Replace(Path.DirectorySeparatorChar, '/'));
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = new FileStream(Resolve(key), FileMode.Open, FileAccess.Read, FileShare.Read, 81_920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Resolve(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public string GetAbsolutePath(string key) => Resolve(key);

    private string Resolve(string key)
    {
        if (Path.IsPathRooted(key)) throw new InvalidOperationException("Storage keys must be relative.");
        var path = Path.GetFullPath(Path.Combine(_root, key));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw new InvalidOperationException("Invalid storage path.");
        return path;
    }
}
