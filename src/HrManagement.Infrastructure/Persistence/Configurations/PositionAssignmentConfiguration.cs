using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class PositionAssignmentConfiguration : EntityConfigurationBase<PositionAssignment>
{
    public override void Configure(EntityTypeBuilder<PositionAssignment> builder)
    {
        base.Configure(builder);
        builder.HasIndex(x => new { x.EmployeeId, x.EffectiveFrom });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
