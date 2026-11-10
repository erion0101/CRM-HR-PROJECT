using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Insights;

public enum OnboardingAttentionReason { OverdueSteps, RepeatedQuizFailures, AwaitingReview, NotStarted }

public sealed record OnboardingInsightsDto(
    int ActiveJourneys,
    int CompletedLast30Days,
    int OverdueJourneys,
    int AwaitingReview,
    decimal? FirstAttemptPassRate,
    decimal? AverageAttemptsToPass,
    IReadOnlyList<OnboardingAttentionItemDto> NeedsAttention,
    IReadOnlyList<OnboardingDocumentDifficultyDto> DocumentDifficulty);

public sealed record OnboardingAttentionItemDto(
    Guid AssignmentId,
    Guid EmployeeId,
    string EmployeeName,
    string JourneyName,
    DateOnly StartDate,
    int OverdueSteps,
    int FailedAttempts,
    IReadOnlyList<OnboardingAttentionReason> Reasons);

/// <summary>How often employees answer questions about a document wrongly. High rates suggest the document is unclear.</summary>
public sealed record OnboardingDocumentDifficultyDto(Guid? DocumentId, string DocumentLabel, int Answers, int IncorrectAnswers, decimal IncorrectRate);

/// <summary>Raw, already scope-filtered rows the calculator aggregates.</summary>
public sealed record InsightJourneyRow(Guid AssignmentId, Guid EmployeeId, string EmployeeName, string JourneyName,
    OnboardingAssignmentStatus Status, DateOnly StartDate, DateTimeOffset? CompletedAtUtc, int OverdueSteps, int PendingReviewSteps);

public sealed record InsightAttemptRow(Guid AssignmentId, int AttemptNumber, OnboardingQuizAttemptStatus Status);

public sealed record InsightAnswerRow(Guid? DocumentId, string DocumentLabel, int Answers, int IncorrectAnswers);

public sealed record OnboardingInsightsData(
    IReadOnlyList<InsightJourneyRow> Journeys,
    IReadOnlyList<InsightAttemptRow> Attempts,
    IReadOnlyList<InsightAnswerRow> Answers);

public interface IOnboardingInsightsReader
{
    /// <summary>Rows for the employees the caller may see (HR: organization; Manager: direct reports).</summary>
    Task<OnboardingInsightsData> ReadAsync(DateOnly today, CancellationToken cancellationToken);
}
