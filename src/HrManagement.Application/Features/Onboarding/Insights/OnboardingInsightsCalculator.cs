using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Insights;

/// <summary>Turns raw onboarding rows into HR metrics. Pure, so the business rules are unit-tested without a database.</summary>
public static class OnboardingInsightsCalculator
{
    /// <summary>A journey still not started this many days after its start date needs a nudge.</summary>
    public const int NotStartedGraceDays = 7;
    public const int RepeatedFailureThreshold = 2;
    /// <summary>Documents with fewer answers are too noisy to rank.</summary>
    public const int MinimumAnswersForDifficulty = 5;
    public const int MaxAttentionItems = 25;
    public const int MaxDocuments = 10;

    private static readonly OnboardingAssignmentStatus[] Finished =
        [OnboardingAssignmentStatus.Completed, OnboardingAssignmentStatus.Cancelled, OnboardingAssignmentStatus.Superseded];

    public static OnboardingInsightsDto Calculate(OnboardingInsightsData data, DateOnly today, DateTimeOffset now)
    {
        var open = data.Journeys.Where(x => !Finished.Contains(x.Status)).ToArray();
        var submitted = data.Attempts.Where(x => x.Status != OnboardingQuizAttemptStatus.InProgress).ToArray();
        var failuresByAssignment = submitted.Where(x => x.Status == OnboardingQuizAttemptStatus.Failed)
            .GroupBy(x => x.AssignmentId).ToDictionary(x => x.Key, x => x.Count());

        var attention = open
            .Select(journey => (journey, reasons: Reasons(journey, failuresByAssignment.GetValueOrDefault(journey.AssignmentId), today)))
            .Where(x => x.reasons.Count > 0)
            .OrderByDescending(x => x.reasons.Count)
            .ThenByDescending(x => x.journey.OverdueSteps)
            .ThenBy(x => x.journey.StartDate)
            .Take(MaxAttentionItems)
            .Select(x => new OnboardingAttentionItemDto(x.journey.AssignmentId, x.journey.EmployeeId, x.journey.EmployeeName,
                x.journey.JourneyName, x.journey.StartDate, x.journey.OverdueSteps,
                failuresByAssignment.GetValueOrDefault(x.journey.AssignmentId), x.reasons))
            .ToArray();

        var byAssignment = submitted.GroupBy(x => x.AssignmentId).ToArray();
        decimal? firstAttemptPassRate = byAssignment.Length == 0 ? null
            : Percent(byAssignment.Count(g => g.Any(x => x.AttemptNumber == 1 && x.Status == OnboardingQuizAttemptStatus.Passed)), byAssignment.Length);
        var attemptsToPass = byAssignment
            .Select(g => g.Where(x => x.Status == OnboardingQuizAttemptStatus.Passed).Select(x => (int?)x.AttemptNumber).Min())
            .OfType<int>().ToArray();
        decimal? averageAttemptsToPass = attemptsToPass.Length == 0 ? null : Math.Round((decimal)attemptsToPass.Average(), 1);

        var documents = data.Answers
            .Where(x => x.Answers >= MinimumAnswersForDifficulty)
            .Select(x => new OnboardingDocumentDifficultyDto(x.DocumentId, x.DocumentLabel, x.Answers, x.IncorrectAnswers,
                Percent(x.IncorrectAnswers, x.Answers)))
            .OrderByDescending(x => x.IncorrectRate).ThenByDescending(x => x.Answers)
            .Take(MaxDocuments).ToArray();

        return new OnboardingInsightsDto(
            open.Length,
            data.Journeys.Count(x => x.Status == OnboardingAssignmentStatus.Completed && x.CompletedAtUtc >= now.AddDays(-30)),
            open.Count(x => x.OverdueSteps > 0),
            open.Count(x => x.PendingReviewSteps > 0),
            firstAttemptPassRate,
            averageAttemptsToPass,
            attention,
            documents);
    }

    private static IReadOnlyList<OnboardingAttentionReason> Reasons(InsightJourneyRow journey, int failedAttempts, DateOnly today)
    {
        var reasons = new List<OnboardingAttentionReason>(4);
        if (journey.OverdueSteps > 0) reasons.Add(OnboardingAttentionReason.OverdueSteps);
        if (failedAttempts >= RepeatedFailureThreshold) reasons.Add(OnboardingAttentionReason.RepeatedQuizFailures);
        if (journey.PendingReviewSteps > 0) reasons.Add(OnboardingAttentionReason.AwaitingReview);
        if (journey.Status == OnboardingAssignmentStatus.NotStarted && journey.StartDate.AddDays(NotStartedGraceDays) <= today)
            reasons.Add(OnboardingAttentionReason.NotStarted);
        return reasons;
    }

    private static decimal Percent(int part, int whole) => whole == 0 ? 0 : Math.Round(part * 100m / whole, 1);
}
