using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Commands.DeleteQuizQuestion;

public sealed class DeleteQuizQuestionCommandHandler(IOnboardingRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteQuizQuestionCommand, OnboardingQuizAdminDto>
{
    public async Task<OnboardingQuizAdminDto> Handle(DeleteQuizQuestionCommand request, CancellationToken cancellationToken)
    {
        var quiz = await repository.GetQuizAsync(request.QuizId, cancellationToken) ?? throw new NotFoundException("Quiz not found.");
        if (quiz.Status != OnboardingQuizStatus.Draft) throw new ConflictException("Published quizzes are immutable.");
        var question = await repository.GetQuestionAsync(quiz.Id, request.QuestionId, cancellationToken) ?? throw new NotFoundException("Question not found.");
        if (question.Version != request.ExpectedVersion) throw new ConflictException("The question was changed by another user.");
        repository.RemoveQuestionOptions(await repository.GetQuestionOptionsAsync(question.Id, cancellationToken));
        repository.RemoveQuestion(question);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetQuizAdminDtoAsync(quiz.Id, cancellationToken) ?? throw new InvalidOperationException("Updated quiz was not available.");
    }
}
