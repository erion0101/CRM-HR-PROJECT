using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class ReportingLineConfiguration : EntityConfigurationBase<ReportingLine>
{
    public override void Configure(EntityTypeBuilder<ReportingLine> builder)
    {
        base.Configure(builder);
        builder.HasIndex(x => new { x.ManagerEmployeeId, x.EffectiveFrom });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
