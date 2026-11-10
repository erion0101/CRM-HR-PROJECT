using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Authorization;
using HrManagement.Application.Common.Interfaces;

namespace HrManagement.Application.Features.Onboarding.Queries.GetOnboardingTemplates;

public sealed class GetOnboardingTemplatesQueryHandler(IOnboardingRepository repository, ICurrentUserService currentUser)
    : IQueryHandler<GetOnboardingTemplatesQuery, IReadOnlyList<OnboardingTemplateDto>>
{
    public Task<IReadOnlyList<OnboardingTemplateDto>> Handle(GetOnboardingTemplatesQuery request, CancellationToken cancellationToken)
    {
        var mayReadDrafts = currentUser.IsInRole(Roles.Hr) || currentUser.IsInRole(Roles.ProductManager);
        return repository.ListTemplatesAsync(request.PublishedOnly || !mayReadDrafts, cancellationToken);
    }
}
