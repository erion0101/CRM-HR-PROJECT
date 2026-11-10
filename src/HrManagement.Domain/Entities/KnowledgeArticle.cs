namespace HrManagement.Domain;

public sealed class KnowledgeArticle : Entity
{
    private KnowledgeArticle() { }

    public string Slug { get; private set; } = string.Empty;
    public Guid OwnerUserId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public KnowledgeAudience Audience { get; private set; }
    public int SortOrder { get; private set; }
    public KnowledgeArticleStatus Status { get; private set; } = KnowledgeArticleStatus.Draft;
    public int LatestRevision { get; private set; } = 1;
    public int? PublishedRevision { get; private set; }
    public DateTimeOffset? LastReviewedAtUtc { get; private set; }

    public bool IsProjectArticle => ProjectId.HasValue;

    public static KnowledgeArticle Create(
        Guid organizationId,
        string slug,
        Guid ownerUserId,
        KnowledgeAudience audience,
        Guid? projectId,
        Guid? departmentId,
        int sortOrder)
    {
        ValidateAudience(audience, projectId, departmentId);
        return new KnowledgeArticle
        {
            OrganizationId = organizationId,
            Slug = NormalizeSlug(slug),
            OwnerUserId = ownerUserId,
            Audience = audience,
            ProjectId = projectId,
            DepartmentId = departmentId,
            SortOrder = Math.Max(0, sortOrder)
        };
    }

    public int AddRevision(Guid ownerUserId, KnowledgeAudience audience, Guid? departmentId, int sortOrder, DateTimeOffset now)
    {
        if (Status == KnowledgeArticleStatus.Archived) throw new DomainException("Archived knowledge cannot be revised.");
        ValidateAudience(audience, ProjectId, departmentId);
        OwnerUserId = ownerUserId;
        Audience = audience;
        DepartmentId = departmentId;
        SortOrder = Math.Max(0, sortOrder);
        LatestRevision++;
        Status = KnowledgeArticleStatus.Draft;
        Touch(now);
        return LatestRevision;
    }

    public void SubmitForReview(DateTimeOffset now)
    {
        if (Status != KnowledgeArticleStatus.Draft) throw new DomainException("Only draft knowledge can be submitted for review.");
        Status = KnowledgeArticleStatus.InReview;
        Touch(now);
    }

    public void Publish(int revision, DateTimeOffset now)
    {
        if (Status is not (KnowledgeArticleStatus.Draft or KnowledgeArticleStatus.InReview))
            throw new DomainException("Only draft or reviewed knowledge can be published.");
        if (revision != LatestRevision) throw new DomainException("Only the latest knowledge revision can be published.");
        PublishedRevision = revision;
        Status = KnowledgeArticleStatus.Published;
        LastReviewedAtUtc = now.ToUniversalTime();
        Touch(now);
    }

    public void ArchiveKnowledge(DateTimeOffset now)
    {
        if (Status == KnowledgeArticleStatus.Archived) return;
        Status = KnowledgeArticleStatus.Archived;
        Touch(now);
    }

    private static void ValidateAudience(KnowledgeAudience audience, Guid? projectId, Guid? departmentId)
    {
        if (audience == KnowledgeAudience.ProjectMembers && projectId is null)
            throw new DomainException("Project-member knowledge must belong to a project.");
        if (audience == KnowledgeAudience.Department && departmentId is null)
            throw new DomainException("Department knowledge must identify a department.");
        if (projectId.HasValue && audience != KnowledgeAudience.ProjectMembers)
            throw new DomainException("Project knowledge must use the project-members audience.");
    }

    private static string NormalizeSlug(string value)
    {
        var slug = Guard.Required(value, nameof(value), 160).Trim().ToLowerInvariant();
        if (slug.Any(character => !(char.IsAsciiLetterOrDigit(character) || character == '-')))
            throw new DomainException("A knowledge slug may contain only letters, numbers, and hyphens.");
        return slug;
    }
}
