namespace HrManagement.Application.Features.Onboarding;

/// <summary>Where a quiz question's facts come from: a document, or a knowledge article (resolved from its revision).</summary>
public sealed record QuizQuestionSource(Guid QuestionId, Guid? DocumentId, Guid? ArticleId);

/// <summary>
/// Decides which questions of a package's reviewed question bank belong to one employee's journey.
/// HR can leave documents out when assigning onboarding; the employee is then only asked about what they were given.
/// A question matches when its source document is a reading item, is attached to a reading article, or when it is
/// about a reading article. Articles are matched by article (not revision), so republishing an article keeps its questions.
/// </summary>
public static class JourneyQuizScope
{
    public static IReadOnlySet<Guid> QuestionIdsFor(IEnumerable<QuizQuestionSource> questions, JourneyReadingSources reading) =>
        questions.Where(question => Matches(question, reading)).Select(x => x.QuestionId).ToHashSet();

    public static bool Matches(QuizQuestionSource question, JourneyReadingSources reading) =>
        (question.DocumentId is { } documentId && reading.DocumentIds.Contains(documentId)) ||
        (question.ArticleId is { } articleId && reading.ArticleIds.Contains(articleId));
}

/// <summary>Documents and articles a journey's reading items cover. Documents attached to a reading article count as covered.</summary>
public sealed record JourneyReadingSources(IReadOnlySet<Guid> DocumentIds, IReadOnlySet<Guid> ArticleIds)
{
    public static JourneyReadingSources From(IEnumerable<(Guid? DocumentId, Guid? ArticleId)> readingItems,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> articleDocuments)
    {
        var items = readingItems.ToArray();
        var articles = items.Select(x => x.ArticleId).OfType<Guid>().ToHashSet();
        var documents = items.Select(x => x.DocumentId).OfType<Guid>()
            .Concat(articles.SelectMany(article => articleDocuments.GetValueOrDefault(article) ?? []))
            .ToHashSet();
        return new JourneyReadingSources(documents, articles);
    }
}
