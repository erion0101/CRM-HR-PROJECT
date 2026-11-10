using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class OnboardingAcknowledgmentConfiguration : EntityConfigurationBase<OnboardingAcknowledgment>
{
    public override void Configure(EntityTypeBuilder<OnboardingAcknowledgment> builder)
    {
        base.Configure(builder);
        builder.HasIndex(x => new { x.StepProgressId, x.KnowledgeRevisionId }).IsUnique();
        builder.HasOne<OnboardingAssignment>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OnboardingStepProgress>().WithMany().HasForeignKey(x => x.StepProgressId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeArticle>().WithMany().HasForeignKey(x => x.KnowledgeArticleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeArticleRevision>().WithMany().HasForeignKey(x => x.KnowledgeRevisionId).OnDelete(DeleteBehavior.Restrict);
    }
}
