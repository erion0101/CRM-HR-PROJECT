using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Onboarding.Insights;

namespace HrManagement.Application.Features.Onboarding.Queries.GetOnboardingInsights;

public sealed record GetOnboardingInsightsQuery : IQuery<OnboardingInsightsDto>;
