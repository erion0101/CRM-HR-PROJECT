using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class OnboardingTemplateStepConfiguration : EntityConfigurationBase<OnboardingTemplateStep>
{
    public override void Configure(EntityTypeBuilder<OnboardingTemplateStep> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.SectionTitle).HasMaxLength(160);
        builder.Property(x => x.Title).HasMaxLength(200);
        builder.Property(x => x.Instructions).HasMaxLength(4_000);
        builder.Property(x => x.ExpectedOutcome).HasMaxLength(1_000);
        builder.Property(x => x.ExternalUrl).HasMaxLength(2_000);
        builder.HasIndex(x => new { x.TemplateVersionId, x.SectionOrder, x.StepOrder });
        builder.HasOne<OnboardingTemplateVersion>().WithMany().HasForeignKey(x => x.TemplateVersionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<OnboardingTemplateStep>().WithMany().HasForeignKey(x => x.DependsOnStepId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeArticle>().WithMany().HasForeignKey(x => x.KnowledgeArticleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeDocument>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
    }
}
