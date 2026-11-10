using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.DeleteQuizQuestion;

public sealed record DeleteQuizQuestionCommand(Guid QuizId, Guid QuestionId, long ExpectedVersion) : ICommand<OnboardingQuizAdminDto>;
