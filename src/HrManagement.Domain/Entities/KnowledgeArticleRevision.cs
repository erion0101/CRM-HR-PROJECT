namespace HrManagement.Domain;

public sealed class KnowledgeArticleRevision : Entity
{
    private KnowledgeArticleRevision() { }

    public Guid ArticleId { get; private set; }
    public int RevisionNumber { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public KnowledgeContentFormat ContentFormat { get; private set; } = KnowledgeContentFormat.Markdown;
    public Guid CreatedByUserId { get; private set; }
    public string? ChangeSummary { get; private set; }

    public static KnowledgeArticleRevision Create(
        Guid organizationId,
        Guid articleId,
        int revisionNumber,
        string title,
        string description,
        string category,
        string content,
        Guid createdByUserId,
        string? changeSummary)
    {
        if (revisionNumber < 1) throw new DomainException("Knowledge revision number must be positive.");
        return new KnowledgeArticleRevision
        {
            OrganizationId = organizationId,
            ArticleId = articleId,
            RevisionNumber = revisionNumber,
            Title = Guard.Required(title, nameof(title), 200),
            Description = Guard.Required(description, nameof(description), 500),
            Category = Guard.Required(category, nameof(category), 100),
            Content = Guard.Required(content, nameof(content), 100_000),
            CreatedByUserId = createdByUserId,
            ChangeSummary = string.IsNullOrWhiteSpace(changeSummary) ? null : Guard.Required(changeSummary, nameof(changeSummary), 500)
        };
    }
}
