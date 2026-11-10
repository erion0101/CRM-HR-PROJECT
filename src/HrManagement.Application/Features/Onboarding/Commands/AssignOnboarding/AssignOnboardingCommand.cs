using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.AssignOnboarding;

public sealed record AssignOnboardingCommand(
    Guid EmployeeId,
    Guid TemplateId,
    Guid? ProjectId,
    Guid? ManagerEmployeeId,
    Guid? MentorEmployeeId,
    DateOnly StartDate,
    /// <summary>
    /// Template reading steps (documents/articles) this employee must read, from the package and its project package.
    /// Null means every reading step. Other step types are always included.
    /// </summary>
    IReadOnlyList<Guid>? ReadingStepIds = null) : ICommand<OnboardingAssignmentDto>;
