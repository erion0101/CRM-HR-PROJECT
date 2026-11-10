namespace HrManagement.Domain;

public sealed class OnboardingQuiz : Entity
{
    private OnboardingQuiz() { }

    public Guid TemplateVersionId { get; private set; }
    public int QuizVersion { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public OnboardingQuizStatus Status { get; private set; } = OnboardingQuizStatus.Draft;
    public OnboardingQuizGenerationSource GenerationSource { get; private set; }
    public int PassingPercentage { get; private set; }
    public int QuestionCount { get; private set; }
    public bool RandomizeQuestions { get; private set; }
    public bool RandomizeOptions { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public Guid? PublishedByUserId { get; private set; }
    public OnboardingQuizGenerationStatus GenerationStatus { get; private set; } = OnboardingQuizGenerationStatus.None;
    public string? GenerationError { get; private set; }
    public Guid? GenerationRequestedByUserId { get; private set; }
    public DateTimeOffset? GenerationLeaseUntilUtc { get; private set; }
    public int GenerationAttempts { get; private set; }

    public const int MaxGenerationAttempts = 3;
    public bool IsGenerating => GenerationStatus is OnboardingQuizGenerationStatus.Queued or OnboardingQuizGenerationStatus.Running;

    public static OnboardingQuiz Create(Guid organizationId, Guid templateVersionId, int quizVersion, string title,
        int passingPercentage, int questionCount, bool randomizeQuestions, bool randomizeOptions,
        OnboardingQuizGenerationSource generationSource)
    {
        if (quizVersion < 1) throw new DomainException("Quiz version must be positive.");
        if (passingPercentage is < 1 or > 100) throw new DomainException("Passing percentage must be between 1 and 100.");
        if (questionCount is < 1 or > 100) throw new DomainException("Question count must be between 1 and 100.");
        return new OnboardingQuiz
        {
            OrganizationId = organizationId,
            TemplateVersionId = templateVersionId,
            QuizVersion = quizVersion,
            Title = Guard.Required(title, nameof(title), 200),
            PassingPercentage = passingPercentage,
            QuestionCount = questionCount,
            RandomizeQuestions = randomizeQuestions,
            RandomizeOptions = randomizeOptions,
            GenerationSource = generationSource
        };
    }

    public void Publish(Guid userId, int availableQuestions, DateTimeOffset now)
    {
        if (Status != OnboardingQuizStatus.Draft) throw new DomainException("Only a draft quiz can be published.");
        if (availableQuestions < QuestionCount) throw new DomainException($"The quiz requires at least {QuestionCount} approved questions.");
        Status = OnboardingQuizStatus.Published;
        PublishedByUserId = userId;
        PublishedAtUtc = now.ToUniversalTime();
        Touch(now);
    }

    public void QueueGeneration(Guid requestedByUserId, DateTimeOffset now)
    {
        if (Status != OnboardingQuizStatus.Draft) throw new DomainException("Only a draft quiz can be AI-generated.");
        if (IsGenerating) throw new DomainException("AI generation is already in progress for this quiz.");
        GenerationStatus = OnboardingQuizGenerationStatus.Queued;
        GenerationRequestedByUserId = requestedByUserId;
        GenerationError = null;
        GenerationLeaseUntilUtc = null;
        GenerationAttempts = 0;
        Touch(now);
    }

    /// <summary>
    /// Claims the job for one worker. A running job whose lease expired (worker crash or timeout) is retried
    /// up to <see cref="MaxGenerationAttempts"/> times, then failed.
    /// </summary>
    public bool TryLeaseGeneration(DateTimeOffset now, TimeSpan leaseDuration)
    {
        var expired = GenerationStatus == OnboardingQuizGenerationStatus.Running && GenerationLeaseUntilUtc < now;
        if (GenerationStatus != OnboardingQuizGenerationStatus.Queued && !expired) return false;
        if (GenerationAttempts >= MaxGenerationAttempts)
        {
            FailGeneration("AI generation did not finish after several attempts. Try again or add questions manually.", now);
            return false;
        }
        GenerationStatus = OnboardingQuizGenerationStatus.Running;
        GenerationLeaseUntilUtc = now.ToUniversalTime().Add(leaseDuration);
        GenerationAttempts++;
        Touch(now);
        return true;
    }

    public void CompleteGeneration(DateTimeOffset now)
    {
        if (Status != OnboardingQuizStatus.Draft) throw new DomainException("Only a draft quiz can be AI-generated.");
        GenerationSource = OnboardingQuizGenerationSource.AiDraft;
        GenerationStatus = OnboardingQuizGenerationStatus.Completed;
        GenerationError = null;
        GenerationLeaseUntilUtc = null;
        Touch(now);
    }

    public void FailGeneration(string reason, DateTimeOffset now)
    {
        GenerationStatus = OnboardingQuizGenerationStatus.Failed;
        GenerationError = Guard.Required(reason.Length > 1_000 ? reason[..1_000] : reason, nameof(reason), 1_000);
        GenerationLeaseUntilUtc = null;
        Touch(now);
    }
}
