using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.SubmitQuizAttempt;

public sealed record SubmitQuizAnswerInput(Guid QuestionId, Guid SelectedOptionId);
public sealed record SubmitQuizAttemptCommand(Guid AttemptId, IReadOnlyList<SubmitQuizAnswerInput> Answers, long ExpectedVersion)
    : ICommand<OnboardingQuizResultDto>;
