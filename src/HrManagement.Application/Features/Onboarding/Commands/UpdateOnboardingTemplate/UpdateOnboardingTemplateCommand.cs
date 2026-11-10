using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.UpdateOnboardingTemplate;

public sealed record UpdateOnboardingTemplateCommand(
    Guid TemplateId,
    string Name,
    string Description,
    Guid? DepartmentId,
    Guid? PositionId,
    int DurationDays,
    IReadOnlyList<OnboardingTemplateStepInput> Steps,
    long ExpectedVersion) : ICommand<OnboardingTemplateDto>;
