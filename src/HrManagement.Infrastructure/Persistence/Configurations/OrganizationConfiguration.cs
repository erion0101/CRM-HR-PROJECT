using HrManagement.Domain;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class OrganizationConfiguration : EntityConfigurationBase<Organization>
{
    public override void Configure(EntityTypeBuilder<Organization> builder)
    {
        base.Configure(builder);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
