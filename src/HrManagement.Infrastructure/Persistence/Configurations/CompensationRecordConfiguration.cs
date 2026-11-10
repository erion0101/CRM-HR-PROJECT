using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class CompensationRecordConfiguration : EntityConfigurationBase<CompensationRecord>
{
    public override void Configure(EntityTypeBuilder<CompensationRecord> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
