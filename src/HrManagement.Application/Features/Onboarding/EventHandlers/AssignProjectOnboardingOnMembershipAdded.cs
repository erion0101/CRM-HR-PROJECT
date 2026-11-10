using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Notifications;
using HrManagement.Application.Features.Projects;
using HrManagement.Application.Features.Projects.Events;
using HrManagement.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HrManagement.Application.Features.Onboarding.EventHandlers;

/// <summary>
/// Onboards an employee onto a project they just joined, using the project's published package:
/// <list type="bullet">
/// <item>If their company journey has not started yet, it is replaced by one combined company + project journey,
/// so there is still a single quiz at the end.</item>
/// <item>Otherwise (journey in progress or finished, or no journey) they get a separate project journey, so existing
/// progress is never disturbed.</item>
/// </list>
/// Authorization comes from the membership itself, which was just created by a user allowed to manage the project;
/// nothing is returned to the caller. Failures are logged and never undo the membership.
/// </summary>
public sealed class AssignProjectOnboardingOnMembershipAdded(
    IOnboardingRepository repository,
    IProjectRepository projects,
    INotificationRepository notifications,
    IUnitOfWork unitOfWork,
    ILogger<AssignProjectOnboardingOnMembershipAdded> logger) : INotificationHandler<ProjectMembershipAddedNotification>
{
    public async Task Handle(ProjectMembershipAddedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            await AssignAsync(notification, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Automatic project onboarding skipped for employee {EmployeeId} on project {ProjectId}.",
                notification.EmployeeId, notification.ProjectId);
        }
    }

    private async Task AssignAsync(ProjectMembershipAddedNotification notification, CancellationToken cancellationToken)
    {
        var package = await repository.GetAssignableProjectPackageAsync(notification.ProjectId, cancellationToken);
        if (package is null) return;
        if (await repository.HasJourneyWithPackageAsync(notification.EmployeeId, package.Id, cancellationToken)) return;

        var project = await projects.GetAsync(notification.ProjectId, cancellationToken);
        var mentorId = project?.OnboardingContactEmployeeId is { } contact && contact != notification.EmployeeId ? contact : (Guid?)null;
        var managerId = await repository.GetCurrentManagerIdAsync(notification.EmployeeId, notification.EffectiveFrom, cancellationToken);
        var factory = new OnboardingJourneyFactory(repository, projects, notifications);

        var notStarted = (await repository.GetOpenAssignmentsAsync(notification.EmployeeId, cancellationToken))
            .FirstOrDefault(x => x.Status == OnboardingAssignmentStatus.NotStarted && x.ProjectId is null && x.ProjectTemplateId is null);
        var mainTemplate = notStarted is null ? null : await repository.GetTemplateAsync(notStarted.TemplateId, cancellationToken);

        if (notStarted is not null && mainTemplate is { Kind: not OnboardingTemplateKind.Project, IsAssignable: true })
        {
            var start = notStarted.StartDate > notification.EffectiveFrom ? notStarted.StartDate : notification.EffectiveFrom;
            await factory.CreateAsync(notification.OrganizationId, new OnboardingJourneyRequest(notification.EmployeeId,
                notStarted.TemplateId, notification.ProjectId, notStarted.ManagerEmployeeId ?? managerId,
                notStarted.MentorEmployeeId ?? mentorId, start), cancellationToken, replaces: notStarted);
        }
        else
        {
            await factory.CreateAsync(notification.OrganizationId, new OnboardingJourneyRequest(notification.EmployeeId,
                package.Id, notification.ProjectId, managerId, mentorId, notification.EffectiveFrom), cancellationToken);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
