using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.UpdateQuizQuestion;

public sealed record UpdateQuizQuestionCommand(Guid QuizId, Guid QuestionId, QuizQuestionInput Question, long ExpectedVersion) : ICommand<OnboardingQuizAdminDto>;
