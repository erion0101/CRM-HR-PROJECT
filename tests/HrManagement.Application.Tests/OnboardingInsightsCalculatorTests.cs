using HrManagement.Application.Features.Onboarding.Insights;
using HrManagement.Domain;
using Xunit;

namespace HrManagement.Application.Tests;

public sealed class OnboardingInsightsCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 11, 10);
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    private static InsightJourneyRow Journey(OnboardingAssignmentStatus status, DateOnly start, int overdue = 0, int review = 0,
        DateTimeOffset? completed = null, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), Guid.NewGuid(), "Ana Person", "Company onboarding", status, start, completed, overdue, review);

    [Fact]
    public void Flags_each_reason_and_ignores_healthy_journeys()
    {
        var failing = Guid.NewGuid();
        var data = new OnboardingInsightsData(
            [
                Journey(OnboardingAssignmentStatus.InProgress, Today.AddDays(-3)),                       // healthy
                Journey(OnboardingAssignmentStatus.InProgress, Today.AddDays(-20), overdue: 2),          // overdue
                Journey(OnboardingAssignmentStatus.PendingReview, Today.AddDays(-5), review: 1),         // awaiting review
                Journey(OnboardingAssignmentStatus.NotStarted, Today.AddDays(-OnboardingInsightsCalculator.NotStartedGraceDays)),
                Journey(OnboardingAssignmentStatus.NotStarted, Today.AddDays(-2)),                       // still within grace
                Journey(OnboardingAssignmentStatus.InProgress, Today.AddDays(-10), id: failing)
            ],
            [
                new(failing, 1, OnboardingQuizAttemptStatus.Failed),
                new(failing, 2, OnboardingQuizAttemptStatus.Failed)
            ],
            []);

        var result = OnboardingInsightsCalculator.Calculate(data, Today, Now);

        Assert.Equal(6, result.ActiveJourneys);
        Assert.Equal(1, result.OverdueJourneys);
        Assert.Equal(1, result.AwaitingReview);
        Assert.Equal(4, result.NeedsAttention.Count);
        Assert.Contains(result.NeedsAttention, x => x.Reasons.Contains(OnboardingAttentionReason.OverdueSteps));
        Assert.Contains(result.NeedsAttention, x => x.Reasons.Contains(OnboardingAttentionReason.AwaitingReview));
        Assert.Contains(result.NeedsAttention, x => x.Reasons.Contains(OnboardingAttentionReason.NotStarted));
        var repeated = Assert.Single(result.NeedsAttention, x => x.AssignmentId == failing);
        Assert.Equal(2, repeated.FailedAttempts);
        Assert.Equal([OnboardingAttentionReason.RepeatedQuizFailures], repeated.Reasons);
    }

    [Fact]
    public void Pass_rates_count_each_journey_once_and_ignore_attempts_in_progress()
    {
        Guid a = Guid.NewGuid(), b = Guid.NewGuid(), c = Guid.NewGuid(), d = Guid.NewGuid();
        var data = new OnboardingInsightsData(
            [
                Journey(OnboardingAssignmentStatus.Completed, Today.AddDays(-40), completed: Now.AddDays(-5)),
                Journey(OnboardingAssignmentStatus.Completed, Today.AddDays(-90), completed: Now.AddDays(-60))
            ],
            [
                new(a, 1, OnboardingQuizAttemptStatus.Passed),
                new(b, 1, OnboardingQuizAttemptStatus.Failed), new(b, 2, OnboardingQuizAttemptStatus.Passed),
                new(c, 1, OnboardingQuizAttemptStatus.Failed), new(c, 2, OnboardingQuizAttemptStatus.Failed),
                new(c, 3, OnboardingQuizAttemptStatus.Passed),
                new(d, 1, OnboardingQuizAttemptStatus.InProgress)
            ],
            []);

        var result = OnboardingInsightsCalculator.Calculate(data, Today, Now);

        Assert.Equal(1, result.CompletedLast30Days);
        Assert.Equal(33.3m, result.FirstAttemptPassRate);
        Assert.Equal(2m, result.AverageAttemptsToPass);
    }

    [Fact]
    public void Document_difficulty_ranks_by_wrong_answer_rate_and_skips_small_samples()
    {
        var data = new OnboardingInsightsData([], [],
        [
            new(Guid.NewGuid(), "Security policy", 20, 12),
            new(Guid.NewGuid(), "Handbook", 40, 4),
            new(Guid.NewGuid(), "Rarely asked", OnboardingInsightsCalculator.MinimumAnswersForDifficulty - 1, 4)
        ]);

        var result = OnboardingInsightsCalculator.Calculate(data, Today, Now);

        Assert.Equal(["Security policy", "Handbook"], result.DocumentDifficulty.Select(x => x.DocumentLabel));
        Assert.Equal(60m, result.DocumentDifficulty[0].IncorrectRate);
        Assert.Null(OnboardingInsightsCalculator.Calculate(new([], [], []), Today, Now).FirstAttemptPassRate);
    }
}
