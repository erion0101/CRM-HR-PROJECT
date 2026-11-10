using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class OnboardingReadingCycleConfiguration : EntityConfigurationBase<OnboardingReadingCycle>
{
    public override void Configure(EntityTypeBuilder<OnboardingReadingCycle> builder)
    {
        base.Configure(builder);
        builder.HasIndex(x => new { x.AssignmentId, x.CycleNumber }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.AssignmentId, x.Status });
        builder.HasOne<OnboardingAssignment>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<OnboardingQuizAttempt>().WithMany().HasForeignKey(x => x.FailedAttemptId).OnDelete(DeleteBehavior.Restrict);
    }
}
