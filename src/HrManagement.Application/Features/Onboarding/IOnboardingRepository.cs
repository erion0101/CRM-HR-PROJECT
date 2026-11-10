using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding;

public interface IOnboardingRepository
{
    void Add(OnboardingTemplate template);
    void Add(OnboardingTemplateVersion version);
    void Add(OnboardingTemplateStep step);
    void Add(OnboardingAssignment assignment);
    void Add(OnboardingStepProgress progress);
    void Add(OnboardingAcknowledgment acknowledgment);
    void Add(OnboardingReadingCycle cycle);
    void Add(OnboardingReadingProgress progress);
    void Add(OnboardingQuiz quiz);
    void Add(OnboardingQuizQuestion question);
    void Add(OnboardingQuizAnswerOption option);
    void Add(OnboardingQuizAttempt attempt);
    void Add(OnboardingQuizAttemptQuestion question);
    void Add(OnboardingQuizAnswer answer);
    Task<OnboardingTemplate?> GetTemplateAsync(Guid id, CancellationToken cancellationToken);
    Task<OnboardingTemplateVersion?> GetTemplateVersionAsync(Guid templateId, int version, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingTemplateStep>> GetTemplateStepsAsync(Guid templateVersionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingTemplateDto>> ListTemplatesAsync(bool publishedOnly, CancellationToken cancellationToken);
    Task<OnboardingAssignment?> GetAssignmentAsync(Guid id, CancellationToken cancellationToken);
    Task<OnboardingStepProgress?> GetStepAsync(Guid assignmentId, Guid stepId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingStepProgress>> GetStepsAsync(Guid assignmentId, CancellationToken cancellationToken);
    Task<bool> HasActiveAssignmentAsync(Guid employeeId, Guid templateId, CancellationToken cancellationToken);
    Task<OnboardingAssignmentDto?> GetAssignmentDtoAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingAssignmentDto>> ListAccessibleAssignmentsAsync(Guid? employeeId, CancellationToken cancellationToken);
    Task<OnboardingDashboardDto?> GetMyDashboardAsync(CancellationToken cancellationToken);
    Task<Guid?> GetPublishedRevisionIdAsync(Guid articleId, CancellationToken cancellationToken);
    Task<bool> AcknowledgmentExistsAsync(Guid stepProgressId, Guid knowledgeRevisionId, CancellationToken cancellationToken);
    Task<bool> IsDocumentEligibleAsync(Guid documentId, CancellationToken cancellationToken);
    Task<bool> DepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken);
    Task<bool> PositionExistsAsync(Guid positionId, CancellationToken cancellationToken);
    Task<Guid?> GetCurrentDocumentVersionIdAsync(Guid documentId, CancellationToken cancellationToken);
    Task<OnboardingQuiz?> GetQuizAsync(Guid quizId, CancellationToken cancellationToken);
    Task<OnboardingQuiz?> GetDraftQuizAsync(Guid templateVersionId, CancellationToken cancellationToken);
    Task<OnboardingQuiz?> GetPublishedQuizAsync(Guid templateVersionId, CancellationToken cancellationToken);
    /// <summary>Published quizzes for the given template versions, in the same order as <paramref name="templateVersionIds"/>.</summary>
    Task<IReadOnlyList<OnboardingQuiz>> GetPublishedQuizzesAsync(IReadOnlyList<Guid> templateVersionIds, CancellationToken cancellationToken);
    Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken cancellationToken);
    Task<bool> HasActiveProjectPackageAsync(Guid projectId, CancellationToken cancellationToken);
    Task<OnboardingTemplate?> GetAssignableProjectPackageAsync(Guid projectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingTemplate>> GetAutoAssignPackagesAsync(CancellationToken cancellationToken);
    /// <summary>True when the employee has any journey that is not cancelled or superseded.</summary>
    Task<bool> HasOpenAssignmentAsync(Guid employeeId, CancellationToken cancellationToken);
    /// <summary>True when any non-cancelled, non-superseded journey (including completed ones) contains the package.</summary>
    Task<bool> HasJourneyWithPackageAsync(Guid employeeId, Guid templateId, CancellationToken cancellationToken);
    /// <summary>Tracked journeys that are not completed, cancelled, or superseded.</summary>
    Task<IReadOnlyList<OnboardingAssignment>> GetOpenAssignmentsAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<string?> GetEmployeeNameAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<Guid?> GetCurrentManagerIdAsync(Guid employeeId, DateOnly asOf, CancellationToken cancellationToken);
    Task<int> GetNextQuizVersionAsync(Guid templateVersionId, CancellationToken cancellationToken);
    Task<OnboardingQuizAdminDto?> GetQuizAdminDtoAsync(Guid quizId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingQuizQuestion>> GetQuizQuestionsAsync(Guid quizId, CancellationToken cancellationToken);
    Task<IReadOnlyList<QuizQuestionSource>> GetQuizQuestionSourcesAsync(IReadOnlyCollection<Guid> quizIds, CancellationToken cancellationToken);
    /// <summary>Clean company/project documents attached to each article.</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetArticleDocumentIdsAsync(IReadOnlyCollection<Guid> articleIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingQuizAnswerOption>> GetQuestionOptionsAsync(Guid questionId, CancellationToken cancellationToken);
    Task<OnboardingQuizQuestion?> GetQuestionAsync(Guid quizId, Guid questionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingQuizAnswerOption>> GetQuestionOptionsAsync(IReadOnlyCollection<Guid> questionIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingQuizQuestion>> GetQuestionsAsync(IReadOnlyCollection<Guid> questionIds, CancellationToken cancellationToken);
    void RemoveQuestionOptions(IReadOnlyCollection<OnboardingQuizAnswerOption> options);
    void RemoveQuestion(OnboardingQuizQuestion question);
    Task<IReadOnlyList<QuizSourceExcerpt>> GetQuizSourceExcerptsAsync(Guid templateVersionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingQuizSourceDto>> GetQuizSourcesAsync(Guid templateVersionId, CancellationToken cancellationToken);
    Task<bool> IsQuizSourceAsync(Guid templateVersionId, Guid? documentId, Guid? documentVersionId, Guid? knowledgeRevisionId, CancellationToken cancellationToken);
    Task<OnboardingReadingCycle?> GetActiveReadingCycleAsync(Guid assignmentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingReadingProgress>> GetReadingProgressesAsync(Guid cycleId, CancellationToken cancellationToken);
    Task<OnboardingReadingProgress?> GetReadingProgressAsync(Guid cycleId, Guid stepProgressId, CancellationToken cancellationToken);
    Task<(EmployeeDocument Document, DocumentVersion Version)?> GetReadingDocumentAsync(Guid cycleId, Guid stepProgressId, CancellationToken cancellationToken);
    Task<OnboardingQuizAttempt?> GetAttemptAsync(Guid attemptId, CancellationToken cancellationToken);
    Task<OnboardingQuizAttempt?> GetInProgressAttemptAsync(Guid assignmentId, Guid cycleId, CancellationToken cancellationToken);
    Task<int> CountAttemptsAsync(Guid assignmentId, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> GetLastSubmittedAttemptQuestionIdsAsync(Guid assignmentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingQuizAttemptQuestion>> GetAttemptQuestionsAsync(Guid attemptId, CancellationToken cancellationToken);
    Task<OnboardingQuizAttemptDto?> GetAttemptDtoAsync(Guid attemptId, bool includeQuestions, CancellationToken cancellationToken);
    void RemoveTemplateSteps(IReadOnlyCollection<OnboardingTemplateStep> steps);
}
