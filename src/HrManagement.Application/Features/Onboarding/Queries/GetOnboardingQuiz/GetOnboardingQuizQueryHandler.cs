using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Queries.GetOnboardingQuiz;

public sealed class GetOnboardingQuizQueryHandler(IOnboardingRepository repository)
    : IQueryHandler<GetOnboardingQuizQuery, OnboardingQuizAdminDto?>
{
    public async Task<OnboardingQuizAdminDto?> Handle(GetOnboardingQuizQuery request, CancellationToken cancellationToken)
    {
        var template = await repository.GetTemplateAsync(request.TemplateId, cancellationToken) ?? throw new NotFoundException("Onboarding template not found.");
        var number = template.PublishedVersion ?? template.CurrentVersion;
        var version = await repository.GetTemplateVersionAsync(template.Id, number, cancellationToken) ?? throw new NotFoundException("Template version not found.");
        var quiz = await repository.GetDraftQuizAsync(version.Id, cancellationToken) ?? await repository.GetPublishedQuizAsync(version.Id, cancellationToken);
        return quiz is null ? null : await repository.GetQuizAdminDtoAsync(quiz.Id, cancellationToken);
    }
}
