using HrManagement.Application.Common.Exceptions;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding;

internal static class QuizQuestionWriter
{
    public static async Task<OnboardingQuizQuestion> AddAsync(IOnboardingRepository repository, OnboardingQuiz quiz,
        QuizQuestionInput input, CancellationToken cancellationToken)
    {
        if (!await repository.IsQuizSourceAsync(quiz.TemplateVersionId, input.SourceDocumentId,
                input.SourceDocumentVersionId, input.SourceKnowledgeRevisionId, cancellationToken))
            throw new ValidationException(new Dictionary<string, string[]> { ["source"] = ["The source is not part of this published onboarding version."] });
        var question = OnboardingQuizQuestion.Create(quiz.OrganizationId, quiz.Id, input.QuestionText, input.Explanation,
            input.Topic, input.Difficulty, input.SortOrder, input.SourceDocumentId, input.SourceDocumentVersionId,
            input.SourceKnowledgeRevisionId, input.SourceLabel, input.SourcePage, input.SourceParagraph);
        repository.Add(question);
        foreach (var option in input.Options.OrderBy(x => x.SortOrder))
            repository.Add(OnboardingQuizAnswerOption.Create(quiz.OrganizationId, question.Id, option.Text, option.IsCorrect, option.SortOrder));
        return question;
    }
}
