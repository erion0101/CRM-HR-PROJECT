using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class AiSourceReferenceConfiguration : EntityConfigurationBase<AiSourceReference>
{
    public override void Configure(EntityTypeBuilder<AiSourceReference> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Kind).HasMaxLength(40);
        builder.Property(x => x.Label).HasMaxLength(200);
        builder.Property(x => x.EntityType).HasMaxLength(80);
        builder.Property(x => x.Location).HasMaxLength(100);
        builder.Property(x => x.Url).HasMaxLength(500);
        builder.HasIndex(x => x.MessageId);
        builder.HasIndex(x => new { x.OrganizationId, x.EmployeeId });
        builder.HasIndex(x => new { x.OrganizationId, x.DocumentId });
        builder.HasOne<AiMessage>().WithMany().HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);
    }
}
