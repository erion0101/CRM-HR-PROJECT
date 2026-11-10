using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Ai;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HrManagement.Infrastructure.Ai;

internal sealed class DocumentIndexingProcessor(
    AppDbContext db,
    IDocumentStorageService storage,
    IDocumentTextExtractor extractor,
    IEmbeddingProvider embeddings,
    IAiRuntimeSettings settings,
    ILogger<DocumentIndexingProcessor> logger) : IDocumentIndexingProcessor
{
    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        if (!settings.Value.Enabled || !settings.Value.RealDataApproved || !settings.ProviderConfigured || !settings.UsagePricingConfigured) return 0;
        var jobs = await db.DocumentAiIndexes.Where(x => x.Status == DocumentAiIndexStatus.Queued ||
                (x.Status == DocumentAiIndexStatus.Processing && x.LeaseUntilUtc < DateTimeOffset.UtcNow))
            .OrderBy(x => x.CreatedAtUtc).Take(5).ToListAsync(cancellationToken);
        var processed = 0;
        foreach (var job in jobs)
        {
            if (!job.TryLease(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5))) continue;
            await db.SaveChangesAsync(cancellationToken);
            try
            {
                var document = await db.Documents.SingleAsync(x => x.Id == job.DocumentId, cancellationToken);
                var version = await db.DocumentVersions.SingleAsync(x => x.Id == job.DocumentVersionId, cancellationToken);
                if (!document.AiIndexingApproved || document.Status != DocumentStatus.Clean ||
                    document.DataClassification is DataClassification.Sensitive or DataClassification.HighlyRestricted ||
                    version.VersionNumber != document.CurrentVersion)
                {
                    job.MarkNotEligible(DateTimeOffset.UtcNow);
                    await db.DocumentChunks.Where(x => x.DocumentVersionId == version.Id).ExecuteDeleteAsync(cancellationToken);
                    await db.SaveChangesAsync(cancellationToken);
                    processed++;
                    continue;
                }
                if (!extractor.Supports(version.ContentType, version.OriginalFileName))
                {
                    job.MarkNotEligible(DateTimeOffset.UtcNow);
                    await db.SaveChangesAsync(cancellationToken);
                    processed++;
                    continue;
                }

                await using var stream = await storage.OpenReadAsync(version.StorageKey, cancellationToken);
                var sections = await extractor.ExtractAsync(stream, version.ContentType, version.OriginalFileName, cancellationToken);
                if (sections.Count == 0 || sections.All(x => string.IsNullOrWhiteSpace(x.Text)))
                {
                    if (version.ContentType == "application/pdf") job.NeedsOcr(DateTimeOffset.UtcNow);
                    else job.Fail("empty_document", false, DateTimeOffset.UtcNow);
                    await db.SaveChangesAsync(cancellationToken);
                    processed++;
                    continue;
                }

                var chunks = Chunk(sections);
                var vectors = new List<float[]>(chunks.Count);
                for (var offset = 0; offset < chunks.Count; offset += 16)
                {
                    var batch = chunks.Skip(offset).Take(16).Select(x => x.Text).ToArray();
                    vectors.AddRange(await embeddings.CreateEmbeddingsAsync(batch, cancellationToken));
                }
                await db.DocumentChunks.Where(x => x.DocumentVersionId == version.Id).ExecuteDeleteAsync(cancellationToken);
                for (var index = 0; index < chunks.Count; index++)
                {
                    var chunk = chunks[index];
                    db.DocumentChunks.Add(DocumentChunk.Create(document.OrganizationId, document.Id, version.Id, index,
                        chunk.Text, chunk.TokenCount, chunk.PageNumber, chunk.ParagraphNumber, vectors[index]));
                }
                job.Complete(settings.Value.EmbeddingModel, DateTimeOffset.UtcNow);
                await db.SaveChangesAsync(cancellationToken);
                processed++;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "AI indexing failed for document version {DocumentVersionId}.", job.DocumentVersionId);
                job.Fail("indexing_failed", true, DateTimeOffset.UtcNow);
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        return processed;
    }

    private static IReadOnlyList<PendingChunk> Chunk(IReadOnlyList<ExtractedDocumentSection> sections)
    {
        const int targetWords = 600;
        const int overlapWords = 80;
        var result = new List<PendingChunk>();
        foreach (var section in sections)
        {
            var words = section.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (words.Length == 0) continue;
            for (var start = 0; start < words.Length; start += targetWords - overlapWords)
            {
                var count = Math.Min(targetWords, words.Length - start);
                result.Add(new PendingChunk(string.Join(' ', words, start, count), Math.Max(1, count * 4 / 3), section.PageNumber, section.ParagraphIndex));
                if (start + count >= words.Length) break;
            }
        }
        return result;
    }

    private sealed record PendingChunk(string Text, int TokenCount, int? PageNumber, int? ParagraphNumber);
}
