using HrManagement.Domain;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class ContractVersionConfiguration : EntityConfigurationBase<ContractVersion>
{
    public override void Configure(EntityTypeBuilder<ContractVersion> builder)
    {
        base.Configure(builder);
        builder.HasIndex(x => new { x.ContractId, x.Revision }).IsUnique();
    }
}
