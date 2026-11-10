namespace HrManagement.Domain;

public sealed class OnboardingTemplateVersion : Entity
{
    private OnboardingTemplateVersion() { }

    public Guid TemplateId { get; private set; }
    public int VersionNumber { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public Guid? PublishedByUserId { get; private set; }

    public static OnboardingTemplateVersion Create(Guid organizationId, Guid templateId, int versionNumber)
    {
        if (versionNumber < 1) throw new DomainException("Template version must be positive.");
        return new OnboardingTemplateVersion { OrganizationId = organizationId, TemplateId = templateId, VersionNumber = versionNumber };
    }

    public void Publish(Guid userId, DateTimeOffset now)
    {
        if (PublishedAtUtc.HasValue) return;
        PublishedByUserId = userId;
        PublishedAtUtc = now.ToUniversalTime();
        Touch(now);
    }
}
