using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Commands.UpdateOnboardingStep;

public sealed record UpdateOnboardingStepCommand(
    Guid AssignmentId,
    Guid StepId,
    OnboardingStepStatus Status,
    string? CompletionNote,
    long ExpectedVersion) : ICommand<OnboardingAssignmentDto>;
