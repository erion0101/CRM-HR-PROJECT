using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Commands.UpdateOnboardingTemplate;

public sealed class UpdateOnboardingTemplateCommandHandler(IOnboardingRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateOnboardingTemplateCommand, OnboardingTemplateDto>
{
    public async Task<OnboardingTemplateDto> Handle(UpdateOnboardingTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await repository.GetTemplateAsync(request.TemplateId, cancellationToken) ?? throw new NotFoundException("Onboarding template not found.");
        if (template.Version != request.ExpectedVersion) throw new ConflictException("The onboarding template was changed by another user.");
        if (template.Status == OnboardingTemplateStatus.Retired) throw new ConflictException("A retired onboarding template cannot be revised.");
        if (request.DepartmentId is { } departmentId && !await repository.DepartmentExistsAsync(departmentId, cancellationToken))
            throw new NotFoundException("Organization unit not found.");
        if (request.PositionId is { } positionId && !await repository.PositionExistsAsync(positionId, cancellationToken))
            throw new NotFoundException("Position not found.");
        OnboardingTemplateVersion version;
        if (template.Status == OnboardingTemplateStatus.Published)
        {
            var number = template.BeginNewVersion(DateTimeOffset.UtcNow);
            version = OnboardingTemplateVersion.Create(template.OrganizationId, template.Id, number);
            repository.Add(version);
        }
        else
        {
            version = await repository.GetTemplateVersionAsync(template.Id, template.CurrentVersion, cancellationToken) ?? throw new NotFoundException("Template version not found.");
            repository.RemoveTemplateSteps(await repository.GetTemplateStepsAsync(version.Id, cancellationToken));
        }
        template.UpdateDraft(request.Name, request.Description, request.DepartmentId, request.PositionId, request.DurationDays, DateTimeOffset.UtcNow);
        var created = new Dictionary<int, OnboardingTemplateStep>();
        foreach (var input in request.Steps.OrderBy(x => x.StepOrder))
        {
            if (input.DependsOnStepOrder is { } dependency && !created.ContainsKey(dependency))
                throw new ValidationException(new Dictionary<string, string[]> { ["steps"] = ["A step dependency must reference an earlier step order."] });
            if (input.KnowledgeArticleId is { } articleId && await repository.GetPublishedRevisionIdAsync(articleId, cancellationToken) is null)
                throw new ValidationException(new Dictionary<string, string[]> { ["steps"] = ["Knowledge steps must reference a published article."] });
            if (input.DocumentId is { } documentId && !await repository.IsDocumentEligibleAsync(documentId, cancellationToken))
                throw new ValidationException(new Dictionary<string, string[]> { ["steps"] = ["Document steps must reference approved knowledge documents."] });
            var step = OnboardingTemplateStep.Create(template.OrganizationId, version.Id, input.SectionTitle, input.SectionOrder,
                input.Title, input.Instructions, input.ExpectedOutcome, input.StepType, input.StepOrder, input.DueAfterDays,
                input.IsRequired, input.KnowledgeArticleId, input.DocumentId, input.ExternalUrl,
                input.DependsOnStepOrder is { } order ? created[order].Id : null);
            created.Add(input.StepOrder, step);
            repository.Add(step);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await repository.ListTemplatesAsync(false, cancellationToken)).Single(x => x.Id == template.Id);
    }
}
