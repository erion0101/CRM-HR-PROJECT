using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class OnboardingReadingProgressConfiguration : EntityConfigurationBase<OnboardingReadingProgress>
{
    public override void Configure(EntityTypeBuilder<OnboardingReadingProgress> builder)
    {
        base.Configure(builder);
        builder.HasIndex(x => new { x.ReadingCycleId, x.StepProgressId }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.ReadingCycleId, x.Status });
        builder.HasOne<OnboardingReadingCycle>().WithMany().HasForeignKey(x => x.ReadingCycleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<OnboardingStepProgress>().WithMany().HasForeignKey(x => x.StepProgressId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeDocument>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentVersion>().WithMany().HasForeignKey(x => x.DocumentVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeArticle>().WithMany().HasForeignKey(x => x.KnowledgeArticleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeArticleRevision>().WithMany().HasForeignKey(x => x.KnowledgeRevisionId).OnDelete(DeleteBehavior.Restrict);
    }
}
