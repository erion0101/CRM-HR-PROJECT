using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.SetOnboardingAutoAssign;

/// <summary>Marks (or unmarks) the company package that new employees receive automatically.</summary>
public sealed record SetOnboardingAutoAssignCommand(Guid TemplateId, bool Enabled, long ExpectedVersion) : ICommand<OnboardingTemplateDto>;
