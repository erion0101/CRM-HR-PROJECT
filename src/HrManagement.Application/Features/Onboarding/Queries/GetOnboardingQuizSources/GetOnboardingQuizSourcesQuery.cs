using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Queries.GetOnboardingQuizSources;

public sealed record GetOnboardingQuizSourcesQuery(Guid TemplateId) : IQuery<IReadOnlyList<OnboardingQuizSourceDto>>;
