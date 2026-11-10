using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Projects;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Commands.AssignOnboarding;

/// <summary>
/// Assigns an onboarding journey. When a company (or general) package is assigned together with a project,
/// the project's published package is included automatically: its steps follow the company steps and its
/// question bank joins the single end-of-journey quiz.
/// </summary>
public sealed class AssignOnboardingCommandHandler(
    IOnboardingRepository repository,
    IProjectRepository projects,
    IAuthorizedDataScope scope,
    IUnitOfWork unitOfWork) : ICommandHandler<AssignOnboardingCommand, OnboardingAssignmentDto>
{
    public async Task<OnboardingAssignmentDto> Handle(AssignOnboardingCommand request, CancellationToken cancellationToken)
    {
        if (!await scope.CanAccessEmployeeAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        var template = await repository.GetTemplateAsync(request.TemplateId, cancellationToken) ?? throw new NotFoundException("Onboarding template not found.");
        if (!template.IsAssignable) throw new ConflictException("Only a published onboarding template can be assigned.");
        if (template.Kind == OnboardingTemplateKind.Project && request.ProjectId is { } requested && requested != template.ProjectId)
            throw new ValidationException(new Dictionary<string, string[]> { ["projectId"] = ["A project package can only be assigned for its own project."] });
        if (await repository.HasActiveAssignmentAsync(request.EmployeeId, request.TemplateId, cancellationToken))
            throw new ConflictException("This employee already has an active journey for the selected template.");

        var projectId = template.Kind == OnboardingTemplateKind.Project ? template.ProjectId : request.ProjectId;
        if (projectId is { } project && !await projects.EmployeeHasActiveMembershipAsync(project, request.EmployeeId, request.StartDate, cancellationToken))
            throw new NotFoundException("The employee does not have an active membership in the selected project.");
        if (request.ManagerEmployeeId is { } managerId && !await projects.EmployeeExistsAsync(managerId, cancellationToken)) throw new NotFoundException("Manager not found.");
        if (request.MentorEmployeeId is { } mentorId && !await projects.EmployeeExistsAsync(mentorId, cancellationToken)) throw new NotFoundException("Mentor not found.");

        var version = await GetPublishedVersionAsync(template, cancellationToken);
        var assignment = OnboardingAssignment.Create(scope.OrganizationId, request.EmployeeId, template.Id, version.Id,
            version.VersionNumber, projectId, request.ManagerEmployeeId, request.MentorEmployeeId, request.StartDate);
        repository.Add(assignment);

        var mainSteps = await repository.GetTemplateStepsAsync(version.Id, cancellationToken);
        var progressSteps = new List<OnboardingStepProgress>();
        await SnapshotStepsAsync(assignment, mainSteps, 0, progressSteps, cancellationToken);

        if (template.Kind != OnboardingTemplateKind.Project && projectId is { } packageProjectId &&
            await repository.GetAssignableProjectPackageAsync(packageProjectId, cancellationToken) is { } projectPackage)
        {
            var packageVersion = await GetPublishedVersionAsync(projectPackage, cancellationToken);
            assignment.IncludeProjectPackage(projectPackage.Id, packageVersion.Id, packageVersion.VersionNumber);
            var sectionOffset = mainSteps.Count == 0 ? 0 : mainSteps.Max(x => x.SectionOrder) + 1;
            await SnapshotStepsAsync(assignment, await repository.GetTemplateStepsAsync(packageVersion.Id, cancellationToken),
                sectionOffset, progressSteps, cancellationToken);
        }

        var quizzes = await repository.GetPublishedQuizzesAsync(assignment.PackageVersionIds, cancellationToken);
        if (quizzes.Count > 0)
        {
            var cycle = await OnboardingReadingCycleFactory.CreateAsync(repository, assignment, progressSteps, 1, DateTimeOffset.UtcNow, cancellationToken);
            assignment.ConfigureAssessment(cycle.Id, 1,
                progressSteps.Where(OnboardingReadingCycleFactory.IsReadingStep).All(x => !x.IsRequired), DateTimeOffset.UtcNow);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetAssignmentDtoAsync(assignment.Id, cancellationToken) ?? throw new InvalidOperationException("Assigned onboarding journey was not available.");
    }

    private async Task<OnboardingTemplateVersion> GetPublishedVersionAsync(OnboardingTemplate template, CancellationToken cancellationToken) =>
        await repository.GetTemplateVersionAsync(template.Id, template.PublishedVersion!.Value, cancellationToken)
            ?? throw new NotFoundException("Published template version not found.");

    private async Task SnapshotStepsAsync(OnboardingAssignment assignment, IReadOnlyList<OnboardingTemplateStep> steps, int sectionOffset,
        List<OnboardingStepProgress> progressSteps, CancellationToken cancellationToken)
    {
        foreach (var step in steps)
        {
            var revisionId = step.KnowledgeArticleId is { } articleId ? await repository.GetPublishedRevisionIdAsync(articleId, cancellationToken) : null;
            if (step.KnowledgeArticleId.HasValue && revisionId is null) throw new ConflictException("A referenced knowledge article is no longer published.");
            var progress = OnboardingStepProgress.Create(assignment.OrganizationId, assignment.Id, step, assignment.StartDate, revisionId, sectionOffset);
            progressSteps.Add(progress);
            repository.Add(progress);
        }
    }
}
