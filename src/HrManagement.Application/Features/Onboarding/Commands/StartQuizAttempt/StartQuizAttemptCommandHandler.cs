using System.Security.Cryptography;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Commands.StartQuizAttempt;

public sealed class StartQuizAttemptCommandHandler(IOnboardingRepository repository, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    : ICommandHandler<StartQuizAttemptCommand, OnboardingQuizAttemptDto>
{
    public async Task<OnboardingQuizAttemptDto> Handle(StartQuizAttemptCommand request, CancellationToken cancellationToken)
    {
        var assignment = await repository.GetAssignmentAsync(request.AssignmentId, cancellationToken) ?? throw new NotFoundException("Onboarding journey not found.");
        if (currentUser.EmployeeId != assignment.EmployeeId) throw new NotFoundException("Onboarding journey not found.");
        if (assignment.AssessmentStatus != OnboardingAssessmentStatus.Available)
            throw new ConflictException("The quiz is locked until every required document is marked as read in the current cycle.");
        if (assignment.CurrentReadingCycleId is not { } cycleId) throw new ConflictException("No active reading cycle exists.");
        var cycle = await repository.GetActiveReadingCycleAsync(assignment.Id, cancellationToken) ?? throw new ConflictException("No active reading cycle exists.");
        var reading = await repository.GetReadingProgressesAsync(cycle.Id, cancellationToken);
        if (reading.Any(x => x.IsRequired && x.Status != OnboardingReadingStatus.Read))
            throw new ConflictException("The quiz is locked until every required document is marked as read in the current cycle.");
        var existing = await repository.GetInProgressAttemptAsync(assignment.Id, cycleId, cancellationToken);
        if (existing is not null)
            return await repository.GetAttemptDtoAsync(existing.Id, true, cancellationToken) ?? throw new InvalidOperationException("Quiz attempt was not available.");
        var quizzes = await repository.GetPublishedQuizzesAsync(assignment.PackageVersionIds, cancellationToken);
        if (quizzes.Count == 0) throw new ConflictException("No published quiz is configured.");
        var previouslyShown = await repository.GetLastSubmittedAttemptQuestionIdsAsync(assignment.Id, cancellationToken);

        // One attempt covers every package in the journey: each package's bank contributes its own per-attempt count.
        var chosen = new List<(OnboardingQuizQuestion Question, bool RandomizeOptions)>();
        foreach (var quiz in quizzes)
        {
            var pool = await repository.GetQuizQuestionsAsync(quiz.Id, cancellationToken);
            if (pool.Count < quiz.QuestionCount) throw new ConflictException("The published quiz no longer has enough approved questions.");
            chosen.AddRange(QuizQuestionSelector.Select(pool, quiz.QuestionCount, previouslyShown, quiz.RandomizeQuestions, RandomNumberGenerator.GetInt32)
                .Select(question => (question, quiz.RandomizeOptions)));
        }
        if (quizzes.Count > 1 && quizzes.Any(x => x.RandomizeQuestions)) Shuffle(chosen);

        var optionsByQuestion = (await repository.GetQuestionOptionsAsync(chosen.Select(x => x.Question.Id).ToArray(), cancellationToken))
            .ToLookup(x => x.QuestionId, x => x.Id);
        var attempt = OnboardingQuizAttempt.Create(assignment.OrganizationId, assignment.Id, cycle.Id, quizzes[0].Id, assignment.EmployeeId,
            await repository.CountAttemptsAsync(assignment.Id, cancellationToken) + 1, CombinedAssessment.PassingPercentage(quizzes), DateTimeOffset.UtcNow);
        repository.Add(attempt);
        for (var index = 0; index < chosen.Count; index++)
        {
            var (question, randomizeOptions) = chosen[index];
            var options = optionsByQuestion[question.Id].ToList();
            if (randomizeOptions) Shuffle(options);
            repository.Add(OnboardingQuizAttemptQuestion.Create(assignment.OrganizationId, attempt.Id, question.Id, index, options));
        }
        assignment.StartAssessment(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetAttemptDtoAsync(attempt.Id, true, cancellationToken) ?? throw new InvalidOperationException("Started quiz attempt was not available.");
    }

    private static void Shuffle<T>(IList<T> values)
    {
        for (var i = values.Count - 1; i > 0; i--)
        {
            var swap = RandomNumberGenerator.GetInt32(i + 1);
            (values[i], values[swap]) = (values[swap], values[i]);
        }
    }
}
