using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Commands.UpdateReadingProgress;

public sealed record UpdateReadingProgressCommand(Guid AssignmentId, Guid StepProgressId, OnboardingReadingStatus Status,
    long ExpectedVersion) : ICommand<OnboardingAssignmentDto>;
