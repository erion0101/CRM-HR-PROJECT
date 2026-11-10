using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.PublishOnboardingQuiz;

public sealed class PublishOnboardingQuizCommandHandler(IOnboardingRepository repository, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    : ICommandHandler<PublishOnboardingQuizCommand, OnboardingQuizAdminDto>
{
    public async Task<OnboardingQuizAdminDto> Handle(PublishOnboardingQuizCommand request, CancellationToken cancellationToken)
    {
        var quiz = await repository.GetQuizAsync(request.QuizId, cancellationToken) ?? throw new NotFoundException("Quiz not found.");
        if (quiz.Version != request.ExpectedVersion) throw new ConflictException("The quiz was changed by another user.");
        var questions = await repository.GetQuizQuestionsAsync(quiz.Id, cancellationToken);
        var optionsByQuestion = (await repository.GetQuestionOptionsAsync(questions.Select(x => x.Id).ToArray(), cancellationToken))
            .ToLookup(x => x.QuestionId);
        foreach (var question in questions)
        {
            var options = optionsByQuestion[question.Id].ToArray();
            if (options.Length != 4 || options.Count(x => x.IsCorrect) != 1)
                throw new ConflictException("Every quiz question must have four options and exactly one correct answer.");
        }
        quiz.Publish(currentUser.UserId ?? throw new ForbiddenException(), questions.Count, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetQuizAdminDtoAsync(quiz.Id, cancellationToken) ?? throw new InvalidOperationException("Published quiz was not available.");
    }
}
