using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Queries.GetOnboardingQuizSources;

public sealed class GetOnboardingQuizSourcesQueryHandler(IOnboardingRepository repository)
    : IQueryHandler<GetOnboardingQuizSourcesQuery, IReadOnlyList<OnboardingQuizSourceDto>>
{
    public async Task<IReadOnlyList<OnboardingQuizSourceDto>> Handle(GetOnboardingQuizSourcesQuery request, CancellationToken cancellationToken)
    {
        var template = await repository.GetTemplateAsync(request.TemplateId, cancellationToken) ?? throw new NotFoundException("Onboarding template not found.");
        var number = template.PublishedVersion ?? throw new ConflictException("Publish the onboarding template before configuring a quiz.");
        var version = await repository.GetTemplateVersionAsync(template.Id, number, cancellationToken) ?? throw new NotFoundException("Template version not found.");
        return await repository.GetQuizSourcesAsync(version.Id, cancellationToken);
    }
}
