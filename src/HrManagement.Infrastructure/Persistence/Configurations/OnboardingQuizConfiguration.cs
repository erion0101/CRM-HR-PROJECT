using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class OnboardingQuizConfiguration : EntityConfigurationBase<OnboardingQuiz>
{
    public override void Configure(EntityTypeBuilder<OnboardingQuiz> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Title).HasMaxLength(200);
        builder.HasIndex(x => new { x.TemplateVersionId, x.QuizVersion }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.TemplateVersionId, x.Status });
        builder.Property(x => x.GenerationError).HasMaxLength(1_000);
        // The worker scans across organizations for queued or stalled jobs.
        builder.HasIndex(x => new { x.GenerationStatus, x.GenerationLeaseUntilUtc });
        builder.HasOne<OnboardingTemplateVersion>().WithMany().HasForeignKey(x => x.TemplateVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}
