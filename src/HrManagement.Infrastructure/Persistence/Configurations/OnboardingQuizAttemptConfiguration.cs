using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class OnboardingQuizAttemptConfiguration : EntityConfigurationBase<OnboardingQuizAttempt>
{
    public override void Configure(EntityTypeBuilder<OnboardingQuizAttempt> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.ScorePercentage).HasPrecision(5, 2);
        builder.HasIndex(x => new { x.AssignmentId, x.AttemptNumber }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.EmployeeId, x.Status });
        builder.HasOne<OnboardingAssignment>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OnboardingReadingCycle>().WithMany().HasForeignKey(x => x.ReadingCycleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OnboardingQuiz>().WithMany().HasForeignKey(x => x.QuizId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
