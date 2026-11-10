using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class AiMessageConfiguration : EntityConfigurationBase<AiMessage>
{
    public override void Configure(EntityTypeBuilder<AiMessage> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ResultType).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.EvidenceStatus).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.Content).HasMaxLength(20_000);
        builder.Property(x => x.ResultJson).HasColumnType("jsonb");
        builder.Property(x => x.Provider).HasMaxLength(40);
        builder.Property(x => x.Model).HasMaxLength(100);
        builder.HasIndex(x => new { x.ConversationId, x.CreatedAtUtc });
        builder.HasOne<AiConversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
    }
}
