using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Features.Employees.Events;
using HrManagement.Application.Features.Onboarding.Commands.AssignOnboarding;
using HrManagement.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HrManagement.Application.Features.Onboarding.EventHandlers;

/// <summary>
/// Gives a newly active employee the default company onboarding package. Uses the normal assignment use case,
/// so every rule (scope, published version, reading cycle, quiz) is identical to a manual assignment.
/// A failure here must never undo the employment change, so it is logged and HR can assign manually.
/// </summary>
public sealed class AssignDefaultOnboardingOnEmploymentActivated(
    IOnboardingRepository repository,
    ISender sender,
    ILogger<AssignDefaultOnboardingOnEmploymentActivated> logger) : INotificationHandler<EmploymentActivatedNotification>
{
    public async Task Handle(EmploymentActivatedNotification notification, CancellationToken cancellationToken)
    {
        var package = (await repository.GetAutoAssignPackagesAsync(cancellationToken)).FirstOrDefault(x => x.IsAssignable);
        if (package is null) return;
        if (await repository.HasOpenAssignmentAsync(notification.EmployeeId, cancellationToken)) return;

        var managerId = await repository.GetCurrentManagerIdAsync(notification.EmployeeId, notification.StartDate, cancellationToken);
        // A new hire already added to a project gets that project's documents in the same journey.
        var projectId = (await repository.GetProjectsWithPackageAsync(notification.EmployeeId, notification.StartDate, cancellationToken))
            .Cast<Guid?>().FirstOrDefault();
        try
        {
            await sender.Send(new AssignOnboardingCommand(notification.EmployeeId, package.Id, projectId, managerId, null,
                notification.StartDate), cancellationToken);
        }
        catch (Exception exception) when (exception is ConflictException or NotFoundException or ValidationException or DomainException)
        {
            logger.LogWarning(exception, "Automatic onboarding assignment skipped for employee {EmployeeId}.", notification.EmployeeId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The employment is already saved; report the problem without failing HR's request.
            logger.LogError(exception, "Automatic onboarding assignment failed for employee {EmployeeId}.", notification.EmployeeId);
        }
    }
}
