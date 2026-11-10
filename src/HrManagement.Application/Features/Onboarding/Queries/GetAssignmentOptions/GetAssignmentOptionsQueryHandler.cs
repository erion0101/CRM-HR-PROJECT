using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Queries.GetAssignmentOptions;

public sealed class GetAssignmentOptionsQueryHandler(IOnboardingRepository repository)
    : IQueryHandler<GetAssignmentOptionsQuery, OnboardingAssignmentOptionsDto>
{
    public async Task<OnboardingAssignmentOptionsDto> Handle(GetAssignmentOptionsQuery request, CancellationToken cancellationToken)
    {
        var template = await repository.GetTemplateAsync(request.TemplateId, cancellationToken) ?? throw new NotFoundException("Onboarding template not found.");
        if (!template.IsAssignable) throw new ConflictException("Only a published onboarding template can be assigned.");
        var packages = new List<OnboardingTemplate> { template };
        if (template.Kind != OnboardingTemplateKind.Project && request.ProjectId is { } projectId &&
            await repository.GetAssignableProjectPackageAsync(projectId, cancellationToken) is { } projectPackage)
            packages.Add(projectPackage);

        var result = new List<OnboardingPackageOptionDto>(packages.Count);
        foreach (var package in packages)
        {
            var version = await repository.GetTemplateVersionAsync(package.Id, package.PublishedVersion!.Value, cancellationToken)
                ?? throw new NotFoundException("Published template version not found.");
            var reading = (await repository.GetTemplateStepsAsync(version.Id, cancellationToken))
                .Where(x => x.StepType is OnboardingStepType.ReadDocument or OnboardingStepType.ReadArticle or OnboardingStepType.PolicyAcknowledgment)
                .ToArray();
            var quiz = await repository.GetPublishedQuizAsync(version.Id, cancellationToken);
            var questions = quiz is null ? [] : await repository.GetQuizQuestionSourcesAsync([quiz.Id], cancellationToken);
            var articleDocuments = await repository.GetArticleDocumentIdsAsync(
                reading.Select(x => x.KnowledgeArticleId).OfType<Guid>().Distinct().ToArray(), cancellationToken);
            result.Add(new OnboardingPackageOptionDto(package.Id, package.Name, package.Kind, quiz is not null, quiz?.QuestionCount ?? 0,
                reading.Select(step =>
                {
                    var sources = JourneyReadingSources.From([(step.DocumentId, step.KnowledgeArticleId)], articleDocuments);
                    return new OnboardingReadingOptionDto(step.Id, step.SectionTitle, step.Title, step.IsRequired, step.DocumentId,
                        step.KnowledgeArticleId, questions.Count(question => JourneyQuizScope.Matches(question, sources)));
                }).ToArray()));
        }
        return new OnboardingAssignmentOptionsDto(result);
    }
}
