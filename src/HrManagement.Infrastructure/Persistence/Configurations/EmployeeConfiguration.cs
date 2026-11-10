using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration : EntityConfigurationBase<Employee>
{
    public override void Configure(EntityTypeBuilder<Employee> builder)
    {
        base.Configure(builder);
        builder.HasIndex(x => new { x.OrganizationId, x.EmployeeNumber }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.WorkEmail }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.LastName, x.FirstName });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
