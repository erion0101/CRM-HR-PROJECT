using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class DocumentAiIndexConfiguration : EntityConfigurationBase<DocumentAiIndex>
{
    public override void Configure(EntityTypeBuilder<DocumentAiIndex> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.PipelineVersion).HasMaxLength(40);
        builder.Property(x => x.EmbeddingModel).HasMaxLength(100);
        builder.Property(x => x.ErrorCode).HasMaxLength(100);
        builder.HasIndex(x => x.DocumentVersionId).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.Status, x.CreatedAtUtc });
        builder.HasOne<EmployeeDocument>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<DocumentVersion>().WithMany().HasForeignKey(x => x.DocumentVersionId).OnDelete(DeleteBehavior.Cascade);
    }
}
