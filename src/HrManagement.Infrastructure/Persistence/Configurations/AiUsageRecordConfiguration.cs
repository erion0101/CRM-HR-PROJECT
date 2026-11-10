using HrManagement.Domain;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class AiUsageRecordConfiguration : EntityConfigurationBase<AiUsageRecord>
{
    public override void Configure(EntityTypeBuilder<AiUsageRecord> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Model).HasMaxLength(100);
        builder.Property(x => x.EstimatedCostUsd).HasPrecision(18, 8);
        builder.HasIndex(x => new { x.OrganizationId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
    }
}
