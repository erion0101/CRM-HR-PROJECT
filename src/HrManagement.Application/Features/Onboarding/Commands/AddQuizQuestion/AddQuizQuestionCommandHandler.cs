using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Commands.AddQuizQuestion;

public sealed class AddQuizQuestionCommandHandler(IOnboardingRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<AddQuizQuestionCommand, OnboardingQuizAdminDto>
{
    public async Task<OnboardingQuizAdminDto> Handle(AddQuizQuestionCommand request, CancellationToken cancellationToken)
    {
        var quiz = await repository.GetQuizAsync(request.QuizId, cancellationToken) ?? throw new NotFoundException("Quiz not found.");
        if (quiz.Status != OnboardingQuizStatus.Draft) throw new ConflictException("Published quizzes are immutable.");
        await QuizQuestionWriter.AddAsync(repository, quiz, request.Question, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetQuizAdminDtoAsync(quiz.Id, cancellationToken) ?? throw new InvalidOperationException("Updated quiz was not available.");
    }
}
