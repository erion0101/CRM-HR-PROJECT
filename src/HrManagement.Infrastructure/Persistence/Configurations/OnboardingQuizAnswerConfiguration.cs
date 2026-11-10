using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class OnboardingQuizAnswerConfiguration : EntityConfigurationBase<OnboardingQuizAnswer>
{
    public override void Configure(EntityTypeBuilder<OnboardingQuizAnswer> builder)
    {
        base.Configure(builder);
        builder.HasIndex(x => new { x.AttemptId, x.QuestionId }).IsUnique();
        builder.HasOne<OnboardingQuizAttempt>().WithMany().HasForeignKey(x => x.AttemptId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<OnboardingQuizQuestion>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OnboardingQuizAnswerOption>().WithMany().HasForeignKey(x => x.SelectedOptionId).OnDelete(DeleteBehavior.Restrict);
    }
}
