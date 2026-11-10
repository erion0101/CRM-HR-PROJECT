using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.PublishOnboardingQuiz;

public sealed record PublishOnboardingQuizCommand(Guid QuizId, long ExpectedVersion) : ICommand<OnboardingQuizAdminDto>;
