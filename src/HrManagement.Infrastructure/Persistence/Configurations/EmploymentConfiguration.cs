using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class EmploymentConfiguration : EntityConfigurationBase<Employment>
{
    public override void Configure(EntityTypeBuilder<Employment> builder)
    {
        base.Configure(builder);
        builder.HasIndex(x => new { x.OrganizationId, x.EmployeeId, x.Status });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
