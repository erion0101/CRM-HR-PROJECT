namespace HrManagement.Domain;

public sealed class ProjectResourceLink : Entity
{
    private ProjectResourceLink() { }

    public Guid ProjectId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string ResourceType { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    public static ProjectResourceLink Create(Guid organizationId, Guid projectId, string label, string url, string resourceType, int sortOrder)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || parsed.Scheme is not ("https" or "http"))
            throw new DomainException("Project resource URL must be an absolute HTTP or HTTPS URL.");
        return new ProjectResourceLink
        {
            OrganizationId = organizationId,
            ProjectId = projectId,
            Label = Guard.Required(label, nameof(label), 120),
            Url = Guard.Required(url, nameof(url), 2_000),
            ResourceType = Guard.Required(resourceType, nameof(resourceType), 80),
            SortOrder = Math.Max(0, sortOrder)
        };
    }
}
