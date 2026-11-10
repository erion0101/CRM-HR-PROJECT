using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : EntityConfigurationBase<Notification>
{
    public override void Configure(EntityTypeBuilder<Notification> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Title).HasMaxLength(200);
        builder.Property(x => x.Message).HasMaxLength(1_000);
        builder.Property(x => x.LinkPath).HasMaxLength(300);
        // Serves both the newest-first list and the unread count for one recipient.
        builder.HasIndex(x => new { x.OrganizationId, x.RecipientEmployeeId, x.CreatedAtUtc });
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.RecipientEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
