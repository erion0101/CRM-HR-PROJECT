using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class DocumentFolderConfiguration : EntityConfigurationBase<DocumentFolder>
{
    public override void Configure(EntityTypeBuilder<DocumentFolder> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Name).HasMaxLength(120);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.AllowedRoles).HasColumnType("text[]");
        builder.HasIndex(x => new { x.OrganizationId, x.Name });
        builder.HasIndex(x => x.EmployeeId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
