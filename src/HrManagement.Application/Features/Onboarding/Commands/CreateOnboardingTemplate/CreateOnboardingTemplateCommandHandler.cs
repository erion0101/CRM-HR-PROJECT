using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Commands.CreateOnboardingTemplate;

public sealed class CreateOnboardingTemplateCommandHandler(IOnboardingRepository repository, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateOnboardingTemplateCommand, OnboardingTemplateDto>
{
    public async Task<OnboardingTemplateDto> Handle(CreateOnboardingTemplateCommand request, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        await ValidateTargetsAsync(request.DepartmentId, request.PositionId, cancellationToken);
        if (request.ProjectId is { } projectId)
        {
            if (!await repository.ProjectExistsAsync(projectId, cancellationToken)) throw new NotFoundException("Project not found.");
            if (await repository.HasActiveProjectPackageAsync(projectId, cancellationToken))
                throw new ConflictException("This project already has an onboarding package. Publish a new version of it instead of creating another.");
        }
        var template = OnboardingTemplate.Create(organizationId, request.Name, request.Description, request.DepartmentId,
            request.PositionId, request.DurationDays, request.Kind, request.ProjectId);
        var version = OnboardingTemplateVersion.Create(organizationId, template.Id, 1);
        repository.Add(template);
        repository.Add(version);
        await AddStepsAsync(organizationId, version.Id, request.Steps, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await repository.ListTemplatesAsync(false, cancellationToken)).Single(x => x.Id == template.Id);
    }

    private async Task ValidateTargetsAsync(Guid? departmentId, Guid? positionId, CancellationToken cancellationToken)
    {
        if (departmentId is { } department && !await repository.DepartmentExistsAsync(department, cancellationToken))
            throw new NotFoundException("Organization unit not found.");
        if (positionId is { } position && !await repository.PositionExistsAsync(position, cancellationToken))
            throw new NotFoundException("Position not found.");
    }

    private async Task AddStepsAsync(Guid organizationId, Guid versionId, IReadOnlyList<OnboardingTemplateStepInput> inputs, CancellationToken cancellationToken)
    {
        var created = new Dictionary<int, OnboardingTemplateStep>();
        foreach (var input in inputs.OrderBy(x => x.StepOrder))
        {
            if (input.DependsOnStepOrder is { } dependencyOrder && !created.ContainsKey(dependencyOrder))
                throw new ValidationException(new Dictionary<string, string[]> { ["steps"] = ["A step dependency must reference an earlier step order."] });
            if (input.KnowledgeArticleId is { } articleId && await repository.GetPublishedRevisionIdAsync(articleId, cancellationToken) is null)
                throw new ValidationException(new Dictionary<string, string[]> { ["steps"] = ["Knowledge steps must reference a published article."] });
            if (input.DocumentId is { } documentId && !await repository.IsDocumentEligibleAsync(documentId, cancellationToken))
                throw new ValidationException(new Dictionary<string, string[]> { ["steps"] = ["Document steps must reference an approved company or project knowledge document."] });
            var step = OnboardingTemplateStep.Create(organizationId, versionId, input.SectionTitle, input.SectionOrder, input.Title,
                input.Instructions, input.ExpectedOutcome, input.StepType, input.StepOrder, input.DueAfterDays, input.IsRequired,
                input.KnowledgeArticleId, input.DocumentId, input.ExternalUrl,
                input.DependsOnStepOrder is { } order ? created[order].Id : null, input.MinimumReadingMinutes);
            created.Add(input.StepOrder, step);
            repository.Add(step);
        }
    }
}
