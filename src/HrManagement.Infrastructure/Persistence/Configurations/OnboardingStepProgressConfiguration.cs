using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class OnboardingStepProgressConfiguration : EntityConfigurationBase<OnboardingStepProgress>
{
    public override void Configure(EntityTypeBuilder<OnboardingStepProgress> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.SectionTitle).HasMaxLength(160);
        builder.Property(x => x.Title).HasMaxLength(200);
        builder.Property(x => x.Instructions).HasMaxLength(4_000);
        builder.Property(x => x.ExpectedOutcome).HasMaxLength(1_000);
        builder.Property(x => x.ExternalUrl).HasMaxLength(2_000);
        builder.Property(x => x.CompletionNote).HasMaxLength(2_000);
        builder.Property(x => x.ReviewNote).HasMaxLength(2_000);
        builder.HasIndex(x => new { x.AssignmentId, x.TemplateStepId }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.DueDate, x.Status });
        builder.HasOne<OnboardingAssignment>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<OnboardingTemplateStep>().WithMany().HasForeignKey(x => x.TemplateStepId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeArticle>().WithMany().HasForeignKey(x => x.KnowledgeArticleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeArticleRevision>().WithMany().HasForeignKey(x => x.KnowledgeRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeDocument>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
    }
}
