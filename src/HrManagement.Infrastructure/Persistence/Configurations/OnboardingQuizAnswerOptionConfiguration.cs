using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class OnboardingQuizAnswerOptionConfiguration : EntityConfigurationBase<OnboardingQuizAnswerOption>
{
    public override void Configure(EntityTypeBuilder<OnboardingQuizAnswerOption> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Text).HasMaxLength(1_000);
        builder.HasIndex(x => new { x.QuestionId, x.SortOrder }).IsUnique();
        builder.HasOne<OnboardingQuizQuestion>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
    }
}
