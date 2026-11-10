using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding;

public sealed record QuizAnswerOptionInput(string Text, bool IsCorrect, int SortOrder);

public sealed record QuizQuestionInput(
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
    IReadOnlyList<QuizAnswerOptionInput> Options);
