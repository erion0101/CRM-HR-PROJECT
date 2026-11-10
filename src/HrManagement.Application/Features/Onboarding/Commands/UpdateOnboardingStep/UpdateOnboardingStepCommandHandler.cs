using HrManagement.Application.Common.Authorization;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Notifications;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Commands.UpdateOnboardingStep;

public sealed class UpdateOnboardingStepCommandHandler(
    IOnboardingRepository repository,
    IAuthorizedDataScope scope,
    ICurrentUserService currentUser,
    INotificationRepository notifications,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateOnboardingStepCommand, OnboardingAssignmentDto>
{
    public async Task<OnboardingAssignmentDto> Handle(UpdateOnboardingStepCommand request, CancellationToken cancellationToken)
    {
        var assignment = await repository.GetAssignmentAsync(request.AssignmentId, cancellationToken) ?? throw new NotFoundException("Onboarding journey not found.");
        var self = currentUser.EmployeeId == assignment.EmployeeId;
        var hr = currentUser.IsInRole(Roles.Hr);
        if ((!self && !hr) || !await scope.CanAccessEmployeeAsync(assignment.EmployeeId, cancellationToken)) throw new NotFoundException("Onboarding journey not found.");
        if (assignment.Status is OnboardingAssignmentStatus.Cancelled or OnboardingAssignmentStatus.Superseded)
            throw new ConflictException("This onboarding journey is no longer active.");
        var step = await repository.GetStepAsync(assignment.Id, request.StepId, cancellationToken) ?? throw new NotFoundException("Onboarding step not found.");
        if (assignment.AssessmentStatus != OnboardingAssessmentStatus.NotConfigured && OnboardingReadingCycleFactory.IsReadingStep(step))
            throw new ConflictException("Use the reading acknowledgement endpoint for assessed onboarding documents.");
        if (step.Version != request.ExpectedVersion) throw new ConflictException("The onboarding step was changed by another user.");
        var allSteps = await repository.GetStepsAsync(assignment.Id, cancellationToken);
        var dependencyCompleted = step.DependsOnTemplateStepId is null || allSteps.Any(x => x.TemplateStepId == step.DependsOnTemplateStepId && x.Status == OnboardingStepStatus.Completed);
        var wasPendingReview = step.Status == OnboardingStepStatus.PendingReview;
        step.SetProgress(request.Status, request.CompletionNote, currentUser.UserId ?? throw new ForbiddenException(), dependencyCompleted, DateTimeOffset.UtcNow);
        if (step.StepType == OnboardingStepType.PolicyAcknowledgment && step.Status == OnboardingStepStatus.Completed &&
            step.KnowledgeArticleId is { } articleId && step.KnowledgeRevisionId is { } revisionId &&
            !await repository.AcknowledgmentExistsAsync(step.Id, revisionId, cancellationToken))
            repository.Add(OnboardingAcknowledgment.Create(scope.OrganizationId, assignment.Id, step.Id, assignment.EmployeeId, articleId, revisionId, DateTimeOffset.UtcNow));
        var notifier = new OnboardingNotifier(notifications, repository);
        if (!wasPendingReview && step.Status == OnboardingStepStatus.PendingReview)
            await notifier.ReviewRequestedAsync(assignment, step, cancellationToken);
        if (Refresh(assignment, allSteps, DateTimeOffset.UtcNow)) await notifier.JourneyCompletedAsync(assignment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetAssignmentDtoAsync(assignment.Id, cancellationToken) ?? throw new InvalidOperationException("Updated journey was not available.");
    }

    /// <summary>Recalculates the journey status. Returns true when this change completed the journey.</summary>
    internal static bool Refresh(OnboardingAssignment assignment, IReadOnlyList<OnboardingStepProgress> steps, DateTimeOffset now)
    {
        var wasCompleted = assignment.Status == OnboardingAssignmentStatus.Completed;
        var required = steps.Where(x => x.IsRequired).ToArray();
        assignment.RefreshStatus(steps.Any(x => x.Status != OnboardingStepStatus.NotStarted), steps.Any(x => x.Status == OnboardingStepStatus.Blocked),
            steps.Any(x => x.Status == OnboardingStepStatus.PendingReview), required.Length == 0 || required.All(x => x.Status == OnboardingStepStatus.Completed), now);
        return !wasCompleted && assignment.Status == OnboardingAssignmentStatus.Completed;
    }
}
