using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.AddQuizQuestion;

public sealed record AddQuizQuestionCommand(Guid QuizId, QuizQuestionInput Question) : ICommand<OnboardingQuizAdminDto>;
