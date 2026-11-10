using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Queries.GetMyOnboarding;

public sealed record GetMyOnboardingQuery : IQuery<OnboardingDashboardDto>;
