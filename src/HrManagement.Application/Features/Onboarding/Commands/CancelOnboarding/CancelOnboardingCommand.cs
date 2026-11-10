using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.CancelOnboarding;

public sealed record CancelOnboardingCommand(Guid AssignmentId, string Reason, long ExpectedVersion) : ICommand<OnboardingAssignmentDto>;
