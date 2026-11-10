using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class AiToolExecutionConfiguration : EntityConfigurationBase<AiToolExecution>
{
    public override void Configure(EntityTypeBuilder<AiToolExecution> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.ToolName).HasMaxLength(80);
        builder.Property(x => x.InputSummary).HasMaxLength(1_000);
        builder.Property(x => x.FailureCode).HasMaxLength(100);
        builder.HasIndex(x => new { x.MessageId, x.CreatedAtUtc });
        builder.HasOne<AiMessage>().WithMany().HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);
    }
}
