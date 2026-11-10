using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Onboarding.Insights;

namespace HrManagement.Application.Features.Onboarding.Queries.GetOnboardingInsights;

public sealed class GetOnboardingInsightsQueryHandler(IOnboardingInsightsReader reader)
    : IQueryHandler<GetOnboardingInsightsQuery, OnboardingInsightsDto>
{
    public async Task<OnboardingInsightsDto> Handle(GetOnboardingInsightsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        return OnboardingInsightsCalculator.Calculate(await reader.ReadAsync(today, cancellationToken), today, now);
    }
}
