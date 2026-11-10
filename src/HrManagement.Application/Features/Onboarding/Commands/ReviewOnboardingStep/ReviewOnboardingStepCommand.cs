using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.ReviewOnboardingStep;

public sealed record ReviewOnboardingStepCommand(Guid AssignmentId, Guid StepId, bool Approved, string? ReviewNote, long ExpectedVersion)
    : ICommand<OnboardingAssignmentDto>;
