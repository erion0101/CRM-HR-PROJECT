using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Features.Notifications;
using HrManagement.Application.Features.Projects;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding;

public sealed record OnboardingJourneyRequest(
    Guid EmployeeId,
    Guid TemplateId,
    Guid? ProjectId,
    Guid? ManagerEmployeeId,
    Guid? MentorEmployeeId,
    DateOnly StartDate,
    IReadOnlyCollection<Guid>? ReadingStepIds = null);

/// <summary>
/// Builds an onboarding journey: snapshots the published package steps (company first, then the project package),
/// creates the first reading cycle when a quiz exists, and optionally supersedes a not-yet-started journey it replaces.
/// Callers are responsible for authorization and for saving the unit of work. This is shared by the HR assignment
/// command and by automatic assignment triggered from employment and project-membership events.
/// </summary>
internal sealed class OnboardingJourneyFactory(IOnboardingRepository repository, IProjectRepository projects,
    INotificationRepository notifications)
{
    public async Task<OnboardingAssignment> CreateAsync(Guid organizationId, OnboardingJourneyRequest request,
        CancellationToken cancellationToken, OnboardingAssignment? replaces = null)
    {
        var template = await repository.GetTemplateAsync(request.TemplateId, cancellationToken) ?? throw new NotFoundException("Onboarding template not found.");
        if (!template.IsAssignable) throw new ConflictException("Only a published onboarding template can be assigned.");
        if (template.Kind == OnboardingTemplateKind.Project && request.ProjectId is { } requested && requested != template.ProjectId)
            throw new ValidationException(new Dictionary<string, string[]> { ["projectId"] = ["A project package can only be assigned for its own project."] });
        // The journey being replaced is still active in the database until this unit of work is saved.
        if (replaces?.TemplateId != template.Id && await repository.HasActiveAssignmentAsync(request.EmployeeId, request.TemplateId, cancellationToken))
            throw new ConflictException("This employee already has an active journey for the selected template.");

        var projectId = template.Kind == OnboardingTemplateKind.Project ? template.ProjectId : request.ProjectId;
        if (projectId is { } project && !await projects.EmployeeHasActiveMembershipAsync(project, request.EmployeeId, request.StartDate, cancellationToken))
            throw new NotFoundException("The employee does not have an active membership in the selected project.");
        if (request.ManagerEmployeeId is { } managerId && !await projects.EmployeeExistsAsync(managerId, cancellationToken)) throw new NotFoundException("Manager not found.");
        if (request.MentorEmployeeId is { } mentorId && !await projects.EmployeeExistsAsync(mentorId, cancellationToken)) throw new NotFoundException("Mentor not found.");

        replaces?.Supersede(DateTimeOffset.UtcNow);
        var version = await GetPublishedVersionAsync(template, cancellationToken);
        var assignment = OnboardingAssignment.Create(organizationId, request.EmployeeId, template.Id, version.Id,
            version.VersionNumber, projectId, request.ManagerEmployeeId, request.MentorEmployeeId, request.StartDate);
        repository.Add(assignment);

        var mainSteps = await repository.GetTemplateStepsAsync(version.Id, cancellationToken);
        IReadOnlyList<OnboardingTemplateStep> packageSteps = [];
        var packageSectionOffset = mainSteps.Count == 0 ? 0 : mainSteps.Max(x => x.SectionOrder) + 1;
        if (template.Kind != OnboardingTemplateKind.Project && projectId is { } packageProjectId &&
            await repository.GetAssignableProjectPackageAsync(packageProjectId, cancellationToken) is { } projectPackage)
        {
            var packageVersion = await GetPublishedVersionAsync(projectPackage, cancellationToken);
            assignment.IncludeProjectPackage(projectPackage.Id, packageVersion.Id, packageVersion.VersionNumber);
            packageSteps = await repository.GetTemplateStepsAsync(packageVersion.Id, cancellationToken);
        }

        var excluded = ExcludedReadingSteps(mainSteps.Concat(packageSteps).ToArray(), request.ReadingStepIds);
        var progressSteps = new List<OnboardingStepProgress>();
        await SnapshotStepsAsync(assignment, mainSteps, 0, excluded, progressSteps, cancellationToken);
        await SnapshotStepsAsync(assignment, packageSteps, packageSectionOffset, excluded, progressSteps, cancellationToken);

        var notifier = new OnboardingNotifier(notifications, repository);
        notifier.JourneyAssigned(assignment, template.Name);
        var quizzes = await repository.GetPublishedQuizzesAsync(assignment.PackageVersionIds, cancellationToken);
        // Only quiz the employee when the reviewed question bank has questions about the documents they were given.
        var allowed = await JourneyQuizPool.AllowedQuestionIdsAsync(repository, quizzes, progressSteps, cancellationToken);
        if (allowed.Values.Any(x => x.Count > 0))
        {
            var cycle = await OnboardingReadingCycleFactory.CreateAsync(repository, assignment, progressSteps, 1, DateTimeOffset.UtcNow, cancellationToken);
            assignment.ConfigureAssessment(cycle.Id, 1,
                progressSteps.Where(OnboardingReadingCycleFactory.IsReadingStep).All(x => !x.IsRequired), DateTimeOffset.UtcNow);
            if (assignment.AssessmentStatus == OnboardingAssessmentStatus.Available) notifier.QuizUnlocked(assignment);
        }
        return assignment;
    }

    private async Task<OnboardingTemplateVersion> GetPublishedVersionAsync(OnboardingTemplate template, CancellationToken cancellationToken) =>
        await repository.GetTemplateVersionAsync(template.Id, template.PublishedVersion!.Value, cancellationToken)
            ?? throw new NotFoundException("Published template version not found.");

    /// <summary>Reading steps HR left out for this employee. Throws when the selection names a step outside the packages.</summary>
    private static IReadOnlySet<Guid> ExcludedReadingSteps(IReadOnlyList<OnboardingTemplateStep> steps, IReadOnlyCollection<Guid>? selected)
    {
        if (selected is null) return new HashSet<Guid>();
        var readingIds = steps.Where(IsReading).Select(x => x.Id).ToHashSet();
        if (selected.Any(id => !readingIds.Contains(id)))
            throw new ValidationException(new Dictionary<string, string[]> { ["readingStepIds"] = ["Choose documents from the selected onboarding packages."] });
        return readingIds.Except(selected).ToHashSet();
    }

    private static bool IsReading(OnboardingTemplateStep step) =>
        step.StepType is OnboardingStepType.ReadDocument or OnboardingStepType.ReadArticle or OnboardingStepType.PolicyAcknowledgment;

    private async Task SnapshotStepsAsync(OnboardingAssignment assignment, IReadOnlyList<OnboardingTemplateStep> steps, int sectionOffset,
        IReadOnlySet<Guid> excluded, List<OnboardingStepProgress> progressSteps, CancellationToken cancellationToken)
    {
        foreach (var step in steps)
        {
            if (excluded.Contains(step.Id)) continue;
            var revisionId = step.KnowledgeArticleId is { } articleId ? await repository.GetPublishedRevisionIdAsync(articleId, cancellationToken) : null;
            if (step.KnowledgeArticleId.HasValue && revisionId is null) throw new ConflictException("A referenced knowledge article is no longer published.");
            var dropDependency = step.DependsOnStepId is { } dependency && excluded.Contains(dependency);
            var progress = OnboardingStepProgress.Create(assignment.OrganizationId, assignment.Id, step, assignment.StartDate, revisionId,
                sectionOffset, dropDependency);
            progressSteps.Add(progress);
            repository.Add(progress);
        }
    }
}
