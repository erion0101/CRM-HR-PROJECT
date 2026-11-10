using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.PublishOnboardingTemplate;

public sealed class PublishOnboardingTemplateCommandHandler(IOnboardingRepository repository, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    : ICommandHandler<PublishOnboardingTemplateCommand, OnboardingTemplateDto>
{
    public async Task<OnboardingTemplateDto> Handle(PublishOnboardingTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await repository.GetTemplateAsync(request.TemplateId, cancellationToken) ?? throw new NotFoundException("Onboarding template not found.");
        if (template.Version != request.ExpectedVersion) throw new ConflictException("The onboarding template was changed by another user.");
        var version = await repository.GetTemplateVersionAsync(template.Id, template.CurrentVersion, cancellationToken) ?? throw new NotFoundException("Template version not found.");
        var steps = await repository.GetTemplateStepsAsync(version.Id, cancellationToken);
        if (steps.Count == 0) throw new ConflictException("An onboarding template must contain at least one step before publication.");
        foreach (var step in steps.Where(x => x.StepType == HrManagement.Domain.OnboardingStepType.ReadDocument))
            if (step.DocumentId is not { } documentId || !await repository.IsDocumentEligibleAsync(documentId, cancellationToken))
                throw new ConflictException($"Required document '{step.Title}' must finish security processing before publication.");
        template.Publish(DateTimeOffset.UtcNow);
        version.Publish(currentUser.UserId ?? throw new ForbiddenException(), DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await repository.ListTemplatesAsync(false, cancellationToken)).Single(x => x.Id == template.Id);
    }
}
