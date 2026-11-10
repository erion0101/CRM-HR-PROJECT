using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Queries.GetOnboardingTemplates;

public sealed record GetOnboardingTemplatesQuery(bool PublishedOnly) : IQuery<IReadOnlyList<OnboardingTemplateDto>>;
