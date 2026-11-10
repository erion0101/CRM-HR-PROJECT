using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class OnboardingQuizAttemptQuestionConfiguration : EntityConfigurationBase<OnboardingQuizAttemptQuestion>
{
    public override void Configure(EntityTypeBuilder<OnboardingQuizAttemptQuestion> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.OptionOrder).HasMaxLength(4_000);
        builder.HasIndex(x => new { x.AttemptId, x.QuestionId }).IsUnique();
        builder.HasIndex(x => new { x.AttemptId, x.SortOrder }).IsUnique();
        builder.HasOne<OnboardingQuizAttempt>().WithMany().HasForeignKey(x => x.AttemptId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<OnboardingQuizQuestion>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
    }
}
