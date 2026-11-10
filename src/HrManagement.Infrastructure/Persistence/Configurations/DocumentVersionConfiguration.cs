using HrManagement.Domain;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class DocumentVersionConfiguration : EntityConfigurationBase<DocumentVersion>
{
    public override void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        base.Configure(builder);
        builder.HasIndex(x => new { x.DocumentId, x.VersionNumber }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.Sha256 });
    }
}
