using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class AiConversationConfiguration : EntityConfigurationBase<AiConversation>
{
    public override void Configure(EntityTypeBuilder<AiConversation> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Title).HasMaxLength(120);
        builder.Property(x => x.ContextType).HasMaxLength(40);
        builder.Property(x => x.AuthorizationFingerprint).HasMaxLength(64);
        builder.HasIndex(x => new { x.OrganizationId, x.UserId, x.UpdatedAtUtc });
        builder.HasIndex(x => x.ExpiresAtUtc);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
