using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class OnboardingQuizQuestionConfiguration : EntityConfigurationBase<OnboardingQuizQuestion>
{
    public override void Configure(EntityTypeBuilder<OnboardingQuizQuestion> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.QuestionText).HasMaxLength(2_000);
        builder.Property(x => x.Explanation).HasMaxLength(4_000);
        builder.Property(x => x.Topic).HasMaxLength(160);
        builder.Property(x => x.SourceLabel).HasMaxLength(240);
        builder.HasIndex(x => new { x.QuizId, x.SortOrder });
        builder.HasOne<OnboardingQuiz>().WithMany().HasForeignKey(x => x.QuizId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<EmployeeDocument>().WithMany().HasForeignKey(x => x.SourceDocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentVersion>().WithMany().HasForeignKey(x => x.SourceDocumentVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeArticleRevision>().WithMany().HasForeignKey(x => x.SourceKnowledgeRevisionId).OnDelete(DeleteBehavior.Restrict);
    }
}
