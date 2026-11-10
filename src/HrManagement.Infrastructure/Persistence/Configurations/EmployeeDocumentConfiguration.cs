using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeDocumentConfiguration : EntityConfigurationBase<EmployeeDocument>
{
    public override void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        base.Configure(builder);
        builder.HasIndex(x => x.FolderId);
        builder.Property(x => x.DataClassification).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(x => new { x.OrganizationId, x.AiIndexingApproved, x.DataClassification });
        builder.HasOne<DocumentFolder>().WithMany().HasForeignKey(x => x.FolderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AiIndexingApprovedBy).OnDelete(DeleteBehavior.SetNull);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
