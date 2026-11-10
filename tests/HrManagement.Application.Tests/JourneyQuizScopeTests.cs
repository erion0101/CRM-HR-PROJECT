using HrManagement.Application.Features.Onboarding;
using Xunit;

namespace HrManagement.Application.Tests;

public sealed class JourneyQuizScopeTests
{
    private static readonly Guid Handbook = Guid.NewGuid();
    private static readonly Guid SecurityArticle = Guid.NewGuid();
    private static readonly Guid SecurityAttachment = Guid.NewGuid();
    private static readonly Guid LeftOutDocument = Guid.NewGuid();

    private static readonly JourneyReadingSources Reading = JourneyReadingSources.From(
        [(Handbook, null), (null, SecurityArticle)],
        new Dictionary<Guid, IReadOnlyList<Guid>> { [SecurityArticle] = [SecurityAttachment] });

    [Fact]
    public void Includes_questions_about_assigned_documents_articles_and_article_attachments()
    {
        QuizQuestionSource[] questions =
        [
            new(Guid.NewGuid(), Handbook, null),
            new(Guid.NewGuid(), null, SecurityArticle),
            new(Guid.NewGuid(), SecurityAttachment, null)
        ];

        Assert.Equal(3, JourneyQuizScope.QuestionIdsFor(questions, Reading).Count);
    }

    [Fact]
    public void Excludes_questions_about_documents_left_out_of_the_journey()
    {
        var leftOut = new QuizQuestionSource(Guid.NewGuid(), LeftOutDocument, null);
        var otherArticle = new QuizQuestionSource(Guid.NewGuid(), null, Guid.NewGuid());

        Assert.Empty(JourneyQuizScope.QuestionIdsFor([leftOut, otherArticle], Reading));
    }
}
