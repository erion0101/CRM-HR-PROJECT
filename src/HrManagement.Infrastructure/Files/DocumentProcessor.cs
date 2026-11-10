using System.IO.Compression;
using System.Net.Sockets;
using System.Security.Cryptography;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HrManagement.Infrastructure.Files;

internal sealed class DocumentProcessor(AppDbContext db, IDocumentStorageService storage, IDocumentScanner scanner, ILogger<DocumentProcessor> logger) : IDocumentProcessor
{
    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var pending = await (from document in db.Documents
                             join version in db.DocumentVersions on document.Id equals version.DocumentId
                             where document.Status == DocumentStatus.PendingScan && version.VersionNumber == document.CurrentVersion
                             orderby version.CreatedAtUtc
                             select new { Document = document, Version = version }).Take(20).ToListAsync(cancellationToken);
        var processed = 0;
        foreach (var item in pending)
        {
            try
            {
                await using var file = await storage.OpenReadAsync(item.Version.StorageKey, cancellationToken);
                if (!await HasValidSignatureAsync(file, item.Version.ContentType, cancellationToken))
                {
                    item.Version.SetScanResult(new string('0', 64), "File signature does not match PDF or DOCX.", DateTimeOffset.UtcNow);
                    item.Document.Reject(DateTimeOffset.UtcNow);
                    await storage.DeleteAsync(item.Version.StorageKey, cancellationToken);
                    await db.SaveChangesAsync(cancellationToken);
                    processed++;
                    continue;
                }
                file.Position = 0;
                var hash = Convert.ToHexString(await SHA256.HashDataAsync(file, cancellationToken)).ToLowerInvariant();
                file.Position = 0;
                var duplicate = await db.DocumentVersions.AnyAsync(x => x.OrganizationId == item.Version.OrganizationId && x.Id != item.Version.Id && x.Sha256 == hash, cancellationToken);
                if (duplicate)
                {
                    item.Version.SetScanResult(hash, "Duplicate document content.", DateTimeOffset.UtcNow);
                    item.Document.Reject(DateTimeOffset.UtcNow);
                    await storage.DeleteAsync(item.Version.StorageKey, cancellationToken);
                    await db.SaveChangesAsync(cancellationToken);
                    processed++;
                    continue;
                }
                var result = await scanner.ScanAsync(file, cancellationToken);
                item.Version.SetScanResult(hash, result.IsClean ? null : result.Response, DateTimeOffset.UtcNow);
                if (!result.IsClean)
                {
                    item.Document.Reject(DateTimeOffset.UtcNow);
                    await storage.DeleteAsync(item.Version.StorageKey, cancellationToken);
                }
                else
                {
                    var extension = item.Version.ContentType == "application/pdf" ? ".pdf" : ".docx";
                    var key = await storage.MoveToPermanentAsync(item.Version.StorageKey, item.Version.OrganizationId, item.Document.Id, item.Version.Id, extension, cancellationToken);
                    item.Version.FinalizeStorage(key, DateTimeOffset.UtcNow);
                    item.Document.MarkClean(DateTimeOffset.UtcNow);
                    if (!await db.DocumentAiIndexes.AnyAsync(x => x.DocumentVersionId == item.Version.Id, cancellationToken))
                    {
                        db.DocumentAiIndexes.Add(DocumentAiIndex.Create(item.Document.OrganizationId, item.Document.Id, item.Version.Id, item.Document.AiIndexingApproved));
                    }
                }
                await db.SaveChangesAsync(cancellationToken);
                processed++;
            }
            catch (Exception exception) when (exception is IOException or SocketException or OperationCanceledException)
            {
                logger.LogWarning(exception, "Document {DocumentId} could not be processed and will be retried.", item.Document.Id);
                if (cancellationToken.IsCancellationRequested) throw;
            }
        }
        return processed;
    }

    private static async Task<bool> HasValidSignatureAsync(Stream stream, string contentType, CancellationToken cancellationToken)
    {
        var buffer = new byte[5];
        var read = await stream.ReadAsync(buffer, cancellationToken);
        if (contentType == "application/pdf") return read == 5 && buffer.AsSpan().SequenceEqual("%PDF-"u8);
        var zipSignature = read >= 4 && buffer[0] == (byte)'P' && buffer[1] == (byte)'K' && buffer[2] is 3 or 5 or 7 && buffer[3] is 4 or 6 or 8;
        if (!zipSignature) return false;
        try
        {
            stream.Position = 0;
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            return archive.GetEntry("[Content_Types].xml") is not null && archive.GetEntry("word/document.xml") is not null;
        }
        catch (InvalidDataException) { return false; }
    }
}
