using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Commands.UpdateQuizQuestion;

public sealed class UpdateQuizQuestionCommandHandler(IOnboardingRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateQuizQuestionCommand, OnboardingQuizAdminDto>
{
    public async Task<OnboardingQuizAdminDto> Handle(UpdateQuizQuestionCommand request, CancellationToken cancellationToken)
    {
        var quiz = await repository.GetQuizAsync(request.QuizId, cancellationToken) ?? throw new NotFoundException("Quiz not found.");
        if (quiz.Status != OnboardingQuizStatus.Draft) throw new ConflictException("Published quizzes are immutable.");
        var question = await repository.GetQuestionAsync(quiz.Id, request.QuestionId, cancellationToken) ?? throw new NotFoundException("Question not found.");
        if (question.Version != request.ExpectedVersion) throw new ConflictException("The question was changed by another user.");
        var input = request.Question;
        if (!await repository.IsQuizSourceAsync(quiz.TemplateVersionId, input.SourceDocumentId, input.SourceDocumentVersionId, input.SourceKnowledgeRevisionId, cancellationToken))
            throw new ValidationException(new Dictionary<string, string[]> { ["source"] = ["The source is not part of this published onboarding version."] });
        question.Update(input.QuestionText, input.Explanation, input.Topic, input.Difficulty, input.SortOrder,
            input.SourceDocumentId, input.SourceDocumentVersionId, input.SourceKnowledgeRevisionId, input.SourceLabel,
            input.SourcePage, input.SourceParagraph, DateTimeOffset.UtcNow);
        repository.RemoveQuestionOptions(await repository.GetQuestionOptionsAsync(question.Id, cancellationToken));
        foreach (var option in input.Options.OrderBy(x => x.SortOrder))
            repository.Add(OnboardingQuizAnswerOption.Create(quiz.OrganizationId, question.Id, option.Text, option.IsCorrect, option.SortOrder));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetQuizAdminDtoAsync(quiz.Id, cancellationToken) ?? throw new InvalidOperationException("Updated quiz was not available.");
    }
}
