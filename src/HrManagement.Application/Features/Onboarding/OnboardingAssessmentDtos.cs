using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding;

public sealed record OnboardingReadingItemDto(
    Guid Id,
    Guid StepProgressId,
    string SectionTitle,
    string Title,
    bool IsRequired,
    Guid? DocumentId,
    Guid? KnowledgeArticleId,
    OnboardingReadingStatus Status,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? ReadAtUtc,
    long Version,
    int? MinimumReadingMinutes = null,
    DateTimeOffset? ReadableAtUtc = null);

public sealed record OnboardingQuizAttemptSummaryDto(
    Guid Id,
    int AttemptNumber,
    int ReadingCycleNumber,
    OnboardingQuizAttemptStatus Status,
    decimal? ScorePercentage,
    int PassingPercentage,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? SubmittedAtUtc);

public sealed record OnboardingAssessmentDto(
    OnboardingAssessmentStatus Status,
    int ReadingCycleNumber,
    int RequiredDocuments,
    int ReadDocuments,
    int ReadingProgressPercent,
    Guid? QuizId,
    string? QuizTitle,
    int? PassingPercentage,
    IReadOnlyList<OnboardingReadingItemDto> ReadingItems,
    IReadOnlyList<OnboardingQuizAttemptSummaryDto> Attempts);

public sealed record OnboardingQuizOptionAdminDto(Guid Id, string Text, bool IsCorrect, int SortOrder);
public sealed record OnboardingQuizOptionDto(Guid Id, string Text);

public sealed record OnboardingQuizQuestionAdminDto(
    Guid Id,
    string QuestionText,
    string Explanation,
    string Topic,
    OnboardingQuizDifficulty Difficulty,
    int SortOrder,
    Guid? SourceDocumentId,
    Guid? SourceDocumentVersionId,
    Guid? SourceKnowledgeRevisionId,
    string SourceLabel,
    int? SourcePage,
    int? SourceParagraph,
    IReadOnlyList<OnboardingQuizOptionAdminDto> Options,
    long Version);

public sealed record OnboardingQuizAdminDto(
    Guid Id,
    Guid TemplateVersionId,
    int QuizVersion,
    string Title,
    OnboardingQuizStatus Status,
    OnboardingQuizGenerationSource GenerationSource,
    int PassingPercentage,
    int QuestionCount,
    bool RandomizeQuestions,
    bool RandomizeOptions,
    DateTimeOffset? PublishedAtUtc,
    IReadOnlyList<OnboardingQuizQuestionAdminDto> Questions,
    long Version,
    OnboardingQuizGenerationStatus GenerationStatus = OnboardingQuizGenerationStatus.None,
    string? GenerationError = null);

public sealed record OnboardingQuizQuestionDto(Guid Id, string QuestionText, int Number, IReadOnlyList<OnboardingQuizOptionDto> Options);

public sealed record OnboardingQuizAttemptDto(
    Guid Id,
    Guid AssignmentId,
    int AttemptNumber,
    int ReadingCycleNumber,
    OnboardingQuizAttemptStatus Status,
    int PassingPercentage,
    decimal? ScorePercentage,
    int? CorrectAnswers,
    int? TotalQuestions,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    IReadOnlyList<OnboardingQuizQuestionDto> Questions,
    long Version);

public sealed record OnboardingQuizResultDto(
    Guid AttemptId,
    OnboardingQuizAttemptStatus Status,
    decimal ScorePercentage,
    int PassingPercentage,
    int CorrectAnswers,
    int TotalQuestions,
    int ReadingCycleNumber,
    bool ReadingReset,
    string Message,
    IReadOnlyList<OnboardingQuizAnswerReviewDto> AnswerReview,
    IReadOnlyList<OnboardingQuizDocumentScoreDto> DocumentScores,
    OnboardingAssignmentDto Assignment);

/// <summary>Per-document result. Safe to show after a failed attempt because it never reveals correct answers.</summary>
public sealed record OnboardingQuizDocumentScoreDto(string SourceLabel, int CorrectAnswers, int TotalQuestions);

public sealed record OnboardingQuizAnswerReviewDto(
    Guid QuestionId,
    string QuestionText,
    Guid SelectedOptionId,
    string SelectedOptionText,
    Guid CorrectOptionId,
    string CorrectOptionText,
    bool IsCorrect,
    string Explanation,
    string SourceLabel,
    int? SourcePage,
    int? SourceParagraph);

public sealed record QuizSourceExcerpt(string SourceId, Guid DocumentId, Guid DocumentVersionId, string Label,
    string Text, int? PageNumber, int? ParagraphNumber);

public sealed record OnboardingQuizSourceDto(Guid? DocumentId, Guid? DocumentVersionId, Guid? KnowledgeRevisionId,
    string Label, string SectionTitle);
