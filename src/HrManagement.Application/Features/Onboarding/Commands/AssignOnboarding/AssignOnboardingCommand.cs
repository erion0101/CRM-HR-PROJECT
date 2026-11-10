using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.AssignOnboarding;

public sealed record AssignOnboardingCommand(
    Guid EmployeeId,
    Guid TemplateId,
    Guid? ProjectId,
    Guid? ManagerEmployeeId,
    Guid? MentorEmployeeId,
    DateOnly StartDate) : ICommand<OnboardingAssignmentDto>;
