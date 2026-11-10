namespace HrManagement.Infrastructure.Identity;

/// <summary>
/// Scoped identity for background work. A worker job that runs application use cases on behalf of a user
/// sets it once per DI scope; <see cref="HttpCurrentUser"/> then reports that organization and user instead of
/// the (absent) HTTP principal. The job carries no roles and no employee identity, so it can only do what the
/// use case itself permits for that organization.
/// </summary>
internal sealed class BackgroundJobContext
{
    public Guid? OrganizationId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? CorrelationId { get; private set; }
    public bool IsActive => OrganizationId is not null;

    public void RunAs(Guid organizationId, Guid? userId, string correlationId)
    {
        if (IsActive) throw new InvalidOperationException("A background job identity is already set for this scope.");
        OrganizationId = organizationId;
        UserId = userId;
        CorrelationId = correlationId;
    }
}
