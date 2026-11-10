using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Notifications;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Commands.SubmitQuizAttempt;

public sealed class SubmitQuizAttemptCommandHandler(IOnboardingRepository repository, ICurrentUserService currentUser,
    IUnitOfWork unitOfWork, IAuditWriter audit, INotificationRepository notifications) : ICommandHandler<SubmitQuizAttemptCommand, OnboardingQuizResultDto>
{
    public async Task<OnboardingQuizResultDto> Handle(SubmitQuizAttemptCommand request, CancellationToken cancellationToken)
    {
        var attempt = await repository.GetAttemptAsync(request.AttemptId, cancellationToken) ?? throw new NotFoundException("Quiz attempt not found.");
        if (currentUser.EmployeeId != attempt.EmployeeId) throw new NotFoundException("Quiz attempt not found.");
        if (attempt.Version != request.ExpectedVersion) throw new ConflictException("This attempt changed or was already submitted.");
        if (attempt.Status != OnboardingQuizAttemptStatus.InProgress) throw new ConflictException("This attempt has already been submitted.");
        var assignment = await repository.GetAssignmentAsync(attempt.AssignmentId, cancellationToken) ?? throw new NotFoundException("Onboarding journey not found.");
        if (assignment.CurrentReadingCycleId != attempt.ReadingCycleId || assignment.AssessmentStatus != OnboardingAssessmentStatus.InProgress)
            throw new ConflictException("This quiz attempt is no longer active.");
        var cycle = await repository.GetActiveReadingCycleAsync(assignment.Id, cancellationToken) ?? throw new ConflictException("The reading cycle is no longer active.");
        var reading = await repository.GetReadingProgressesAsync(cycle.Id, cancellationToken);
        if (reading.Any(x => x.IsRequired && x.Status != OnboardingReadingStatus.Read))
            throw new ConflictException("Required reading is incomplete for this cycle.");
        var attemptQuestions = await repository.GetAttemptQuestionsAsync(attempt.Id, cancellationToken);
        var supplied = request.Answers.ToDictionary(x => x.QuestionId);
        if (attemptQuestions.Count == 0 || supplied.Count != attemptQuestions.Count || attemptQuestions.Any(x => !supplied.ContainsKey(x.QuestionId)))
            throw new ValidationException(new Dictionary<string, string[]> { ["answers"] = ["Every quiz question must be answered exactly once."] });

        var questionIds = attemptQuestions.Select(x => x.QuestionId).ToArray();
        var optionsByQuestion = (await repository.GetQuestionOptionsAsync(questionIds, cancellationToken)).ToLookup(x => x.QuestionId);
        var questionsById = (await repository.GetQuestionsAsync(questionIds, cancellationToken)).ToDictionary(x => x.Id);

        var correct = 0;
        var answerReview = new List<OnboardingQuizAnswerReviewDto>(attemptQuestions.Count);
        foreach (var question in attemptQuestions)
        {
            var answer = supplied[question.QuestionId];
            var options = optionsByQuestion[question.QuestionId].ToArray();
            var selected = options.SingleOrDefault(x => x.Id == answer.SelectedOptionId)
                ?? throw new ValidationException(new Dictionary<string, string[]> { ["answers"] = ["An answer option does not belong to its question."] });
            var correctOption = options.SingleOrDefault(x => x.IsCorrect)
                ?? throw new InvalidOperationException("The published question has no correct answer.");
            var sourceQuestion = questionsById.GetValueOrDefault(question.QuestionId)
                ?? throw new InvalidOperationException("The published quiz question was not available.");
            if (selected.IsCorrect) correct++;
            repository.Add(OnboardingQuizAnswer.Create(assignment.OrganizationId, attempt.Id, question.QuestionId, selected.Id, selected.IsCorrect));
            answerReview.Add(new OnboardingQuizAnswerReviewDto(question.QuestionId, sourceQuestion.QuestionText,
                selected.Id, selected.Text, correctOption.Id, correctOption.Text, selected.IsCorrect,
                sourceQuestion.Explanation, sourceQuestion.SourceLabel, sourceQuestion.SourcePage, sourceQuestion.SourceParagraph));
        }
        var documentScores = answerReview
            .GroupBy(x => x.SourceLabel, StringComparer.Ordinal)
            .Select(group => new OnboardingQuizDocumentScoreDto(group.Key, group.Count(x => x.IsCorrect), group.Count()))
            .OrderBy(x => (double)x.CorrectAnswers / x.TotalQuestions).ThenBy(x => x.SourceLabel, StringComparer.Ordinal)
            .ToArray();

        var now = DateTimeOffset.UtcNow;
        var passed = attempt.Submit(correct, attemptQuestions.Count, now);
        var nextCycleNumber = cycle.CycleNumber;
        if (passed)
        {
            cycle.Pass(now);
            assignment.PassAssessment(now);
            var steps = await repository.GetStepsAsync(assignment.Id, cancellationToken);
            if (Commands.UpdateOnboardingStep.UpdateOnboardingStepCommandHandler.Refresh(assignment, steps, now))
                await new OnboardingNotifier(notifications, repository).JourneyCompletedAsync(assignment, cancellationToken);
        }
        else
        {
            cycle.Fail(attempt.Id, now);
            var steps = await repository.GetStepsAsync(assignment.Id, cancellationToken);
            foreach (var step in steps) step.ResetForReadingCycle(now);
            nextCycleNumber = cycle.CycleNumber + 1;
            var documentVersionSnapshot = reading.ToDictionary(x => x.StepProgressId, x => x.DocumentVersionId);
            var nextCycle = await OnboardingReadingCycleFactory.CreateAsync(repository, assignment, steps, nextCycleNumber,
                now, cancellationToken, documentVersionSnapshot);
            assignment.RestartAssessment(nextCycle.Id, nextCycleNumber, now);
        }
        await new OnboardingNotifier(notifications, repository).QuizSubmittedAsync(assignment, attempt, passed, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await audit.RecordAccessAsync(passed ? "OnboardingQuizPassed" : "OnboardingQuizFailedAndReadingReset",
            nameof(OnboardingQuizAttempt), attempt.Id.ToString(), cancellationToken);
        var dto = await repository.GetAssignmentDtoAsync(assignment.Id, cancellationToken) ?? throw new InvalidOperationException("Updated journey was not available.");
        var score = attempt.ScorePercentage ?? 0;
        var message = passed
            ? $"You passed with {score:0.##}%. The assessment phase is complete."
            : $"Your score was {score:0.##}%. You need {attempt.PassingPercentage}% to pass. Your reading progress has been reset; review every required document again.";
        // Correct answers are revealed only after a pass. Showing them after a failure would let the
        // employee memorise the answer key instead of rereading the documents.
        return new OnboardingQuizResultDto(attempt.Id, attempt.Status, score, attempt.PassingPercentage, correct,
            attemptQuestions.Count, nextCycleNumber, !passed, message, passed ? answerReview : [], documentScores, dto);
    }
}
