using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class KnowledgeArticleConfiguration : EntityConfigurationBase<KnowledgeArticle>
{
    public override void Configure(EntityTypeBuilder<KnowledgeArticle> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Slug).HasMaxLength(160);
        builder.HasIndex(x => new { x.OrganizationId, x.Slug }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.ProjectId, x.Status, x.SortOrder });
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
