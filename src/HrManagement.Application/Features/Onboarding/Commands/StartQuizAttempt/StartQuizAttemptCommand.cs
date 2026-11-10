using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.StartQuizAttempt;

public sealed record StartQuizAttemptCommand(Guid AssignmentId) : ICommand<OnboardingQuizAttemptDto>;
