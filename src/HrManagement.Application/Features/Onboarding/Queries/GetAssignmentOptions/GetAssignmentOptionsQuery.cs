using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Queries.GetAssignmentOptions;

/// <summary>What HR can choose from when assigning a package (plus the project's package) to one employee.</summary>
public sealed record GetAssignmentOptionsQuery(Guid TemplateId, Guid? ProjectId) : IQuery<OnboardingAssignmentOptionsDto>;

public sealed record OnboardingAssignmentOptionsDto(IReadOnlyList<OnboardingPackageOptionDto> Packages);

public sealed record OnboardingPackageOptionDto(Guid TemplateId, string Name, OnboardingTemplateKind Kind, bool HasQuiz,
    int QuestionsPerAttempt, IReadOnlyList<OnboardingReadingOptionDto> ReadingSteps);

/// <summary>A reading item HR can include or leave out, with the number of reviewed questions about it.</summary>
public sealed record OnboardingReadingOptionDto(Guid TemplateStepId, string SectionTitle, string Title, bool IsRequired,
    Guid? DocumentId, Guid? KnowledgeArticleId, int QuestionCount);
