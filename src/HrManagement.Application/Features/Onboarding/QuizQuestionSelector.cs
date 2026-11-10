using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding;

/// <summary>
/// Chooses the questions for one attempt from the published question bank.
/// Questions are drawn round-robin across source documents so every required document is assessed,
/// and questions shown in the previous attempt are used only when no fresh question remains for that document.
/// </summary>
internal static class QuizQuestionSelector
{
    public static IReadOnlyList<OnboardingQuizQuestion> Select(IReadOnlyList<OnboardingQuizQuestion> pool, int count,
        IReadOnlySet<Guid> previouslyShown, bool randomize, Func<int, int> nextInt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        if (!randomize) return pool.Take(count).ToArray();

        var queues = pool.GroupBy(SourceKey)
            .Select(group =>
            {
                var questions = group.ToList();
                Shuffle(questions, nextInt);
                // Stable sort keeps the shuffled order inside each partition.
                return new Queue<OnboardingQuizQuestion>(questions.OrderBy(x => previouslyShown.Contains(x.Id)));
            })
            .ToList();
        Shuffle(queues, nextInt);

        var selected = new List<OnboardingQuizQuestion>(Math.Min(count, pool.Count));
        while (selected.Count < count && queues.Count > 0)
        {
            for (var index = 0; index < queues.Count && selected.Count < count; index++)
                selected.Add(queues[index].Dequeue());
            queues.RemoveAll(queue => queue.Count == 0);
        }
        Shuffle(selected, nextInt);
        return selected;
    }

    public static string SourceKey(OnboardingQuizQuestion question) =>
        question.SourceDocumentId?.ToString() ?? question.SourceKnowledgeRevisionId?.ToString() ?? question.SourceLabel;

    private static void Shuffle<T>(IList<T> values, Func<int, int> nextInt)
    {
        for (var i = values.Count - 1; i > 0; i--)
        {
            var swap = nextInt(i + 1);
            (values[i], values[swap]) = (values[swap], values[i]);
        }
    }
}
