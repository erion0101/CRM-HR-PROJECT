using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.GenerateOnboardingQuiz;

public sealed record GenerateOnboardingQuizCommand(Guid QuizId) : ICommand<OnboardingQuizAdminDto>;
