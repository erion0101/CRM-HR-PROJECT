using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding;

/// <summary>Loads, per published quiz, the questions that are about this journey's reading items.</summary>
internal static class JourneyQuizPool
{
    public static async Task<IReadOnlyDictionary<Guid, IReadOnlySet<Guid>>> AllowedQuestionIdsAsync(IOnboardingRepository repository,
        IReadOnlyCollection<OnboardingQuiz> quizzes, IEnumerable<OnboardingStepProgress> journeySteps, CancellationToken cancellationToken)
    {
        if (quizzes.Count == 0) return new Dictionary<Guid, IReadOnlySet<Guid>>();
        var reading = journeySteps.Where(OnboardingReadingCycleFactory.IsReadingStep).Select(x => (x.DocumentId, x.KnowledgeArticleId)).ToArray();
        var articleIds = reading.Select(x => x.KnowledgeArticleId).OfType<Guid>().Distinct().ToArray();
        var sources = JourneyReadingSources.From(reading, await repository.GetArticleDocumentIdsAsync(articleIds, cancellationToken));
        var questions = (await repository.GetQuizQuestionSourcesAsync(quizzes.Select(x => x.Id).ToArray(), cancellationToken)).ToArray();
        var questionQuiz = await QuestionQuizIdsAsync(repository, quizzes, cancellationToken);
        var allowed = JourneyQuizScope.QuestionIdsFor(questions, sources);
        return quizzes.ToDictionary(quiz => quiz.Id,
            quiz => (IReadOnlySet<Guid>)allowed.Where(id => questionQuiz.GetValueOrDefault(id) == quiz.Id).ToHashSet());
    }

    private static async Task<Dictionary<Guid, Guid>> QuestionQuizIdsAsync(IOnboardingRepository repository,
        IReadOnlyCollection<OnboardingQuiz> quizzes, CancellationToken cancellationToken)
    {
        var map = new Dictionary<Guid, Guid>();
        foreach (var quiz in quizzes)
            foreach (var question in await repository.GetQuizQuestionsAsync(quiz.Id, cancellationToken))
                map[question.Id] = quiz.Id;
        return map;
    }
}
