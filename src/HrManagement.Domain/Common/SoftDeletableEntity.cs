namespace HrManagement.Domain;

public abstract class SoftDeletableEntity : Entity
{
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAtUtc { get; protected set; }

    public void Archive(DateTimeOffset now)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAtUtc = now.ToUniversalTime();
        Touch(now);
    }
}
