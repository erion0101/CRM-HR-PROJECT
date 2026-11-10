using HrManagement.Domain;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class DocumentAssociationConfiguration : EntityConfigurationBase<DocumentAssociation>
{
    public override void Configure(EntityTypeBuilder<DocumentAssociation> builder)
    {
        base.Configure(builder);
        builder.HasIndex(x => new { x.DocumentId, x.AssociationType, x.AssociatedEntityId }).IsUnique();
    }
}
