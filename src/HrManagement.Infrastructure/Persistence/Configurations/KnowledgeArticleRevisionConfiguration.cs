using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class KnowledgeArticleRevisionConfiguration : EntityConfigurationBase<KnowledgeArticleRevision>
{
    public override void Configure(EntityTypeBuilder<KnowledgeArticleRevision> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Title).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Category).HasMaxLength(100);
        builder.Property(x => x.Content).HasMaxLength(100_000);
        builder.Property(x => x.ChangeSummary).HasMaxLength(500);
        builder.HasIndex(x => new { x.ArticleId, x.RevisionNumber }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.Category });
        builder.HasOne<KnowledgeArticle>().WithMany().HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.Restrict);
    }
}
