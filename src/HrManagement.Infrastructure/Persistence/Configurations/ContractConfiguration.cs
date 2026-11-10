using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class ContractConfiguration : EntityConfigurationBase<Contract>
{
    public override void Configure(EntityTypeBuilder<Contract> builder)
    {
        base.Configure(builder);
        builder.HasIndex(x => new { x.OrganizationId, x.ContractNumber }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.Status, x.EndDate });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
