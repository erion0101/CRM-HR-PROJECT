using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.RunOnboardingQuizGeneration;

public sealed record RunOnboardingQuizGenerationCommand(Guid QuizId) : ICommand;
