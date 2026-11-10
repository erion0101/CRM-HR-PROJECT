using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.PublishOnboardingTemplate;

public sealed record PublishOnboardingTemplateCommand(Guid TemplateId, long ExpectedVersion) : ICommand<OnboardingTemplateDto>;
