namespace HrManagement.Domain;

public sealed class DocumentChunk : Entity
{
    private DocumentChunk() { }

    public Guid DocumentId { get; private set; }
    public Guid DocumentVersionId { get; private set; }
    public int ChunkIndex { get; private set; }
    [AuditRedacted] public string Text { get; private set; } = string.Empty;
    public int TokenCount { get; private set; }
    public int? PageNumber { get; private set; }
    public int? ParagraphNumber { get; private set; }
    [AuditRedacted] public float[] Embedding { get; private set; } = [];

    public static DocumentChunk Create(Guid organizationId, Guid documentId, Guid documentVersionId, int chunkIndex, string text, int tokenCount, int? pageNumber, int? paragraphNumber, float[] embedding) => new()
    {
        OrganizationId = organizationId,
        DocumentId = documentId,
        DocumentVersionId = documentVersionId,
        ChunkIndex = chunkIndex,
        Text = Guard.Required(text, nameof(text), 20_000),
        TokenCount = Math.Max(1, tokenCount),
        PageNumber = pageNumber,
        ParagraphNumber = paragraphNumber,
        Embedding = embedding
    };
}
