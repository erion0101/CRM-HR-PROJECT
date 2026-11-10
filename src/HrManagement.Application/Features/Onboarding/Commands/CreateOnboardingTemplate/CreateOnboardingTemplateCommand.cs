using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Commands.CreateOnboardingTemplate;

public sealed record CreateOnboardingTemplateCommand(
    string Name,
    string Description,
    Guid? DepartmentId,
    Guid? PositionId,
    int DurationDays,
    IReadOnlyList<OnboardingTemplateStepInput> Steps,
    OnboardingTemplateKind Kind = OnboardingTemplateKind.General,
    Guid? ProjectId = null) : ICommand<OnboardingTemplateDto>;
