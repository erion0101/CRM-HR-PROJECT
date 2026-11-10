using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Queries.GetMyOnboarding;

public sealed class GetMyOnboardingQueryHandler(IOnboardingRepository repository) : IQueryHandler<GetMyOnboardingQuery, OnboardingDashboardDto>
{
    public async Task<OnboardingDashboardDto> Handle(GetMyOnboardingQuery request, CancellationToken cancellationToken) =>
        await repository.GetMyDashboardAsync(cancellationToken) ?? throw new NotFoundException("No employee profile is linked to this account.");
}
