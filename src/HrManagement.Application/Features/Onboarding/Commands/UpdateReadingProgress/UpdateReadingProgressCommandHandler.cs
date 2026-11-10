using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Notifications;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Commands.UpdateReadingProgress;

public sealed class UpdateReadingProgressCommandHandler(IOnboardingRepository repository, ICurrentUserService currentUser,
    INotificationRepository notifications, IUnitOfWork unitOfWork) : ICommandHandler<UpdateReadingProgressCommand, OnboardingAssignmentDto>
{
    public async Task<OnboardingAssignmentDto> Handle(UpdateReadingProgressCommand request, CancellationToken cancellationToken)
    {
        var assignment = await repository.GetAssignmentAsync(request.AssignmentId, cancellationToken) ?? throw new NotFoundException("Onboarding journey not found.");
        if (currentUser.EmployeeId != assignment.EmployeeId) throw new NotFoundException("Onboarding journey not found.");
        if (assignment.Status is OnboardingAssignmentStatus.Cancelled or OnboardingAssignmentStatus.Superseded or OnboardingAssignmentStatus.Completed)
            throw new ConflictException("This onboarding journey is not active.");
        if (assignment.CurrentReadingCycleId is not { } cycleId) throw new ConflictException("This onboarding journey has no active assessed reading cycle.");
        var cycle = await repository.GetActiveReadingCycleAsync(assignment.Id, cancellationToken) ?? throw new ConflictException("The reading cycle is no longer active.");
        if (cycle.Id != cycleId) throw new ConflictException("The reading cycle changed. Refresh the journey.");
        var progress = await repository.GetReadingProgressAsync(cycle.Id, request.StepProgressId, cancellationToken) ?? throw new NotFoundException("Reading item not found.");
        if (progress.Version != request.ExpectedVersion) throw new ConflictException("The reading item was changed. Refresh the journey.");
        var step = await repository.GetStepAsync(assignment.Id, request.StepProgressId, cancellationToken) ?? throw new NotFoundException("Onboarding step not found.");
        var now = DateTimeOffset.UtcNow;
        if (request.Status == OnboardingReadingStatus.InProgress)
        {
            progress.Start(now);
            step.SetProgress(OnboardingStepStatus.InProgress, null, currentUser.UserId ?? throw new ForbiddenException(), true, now);
        }
        else
        {
            progress.MarkRead(now);
            step.SetProgress(OnboardingStepStatus.Completed, "Employee acknowledged this required reading.", currentUser.UserId ?? throw new ForbiddenException(), true, now);
            if (step.StepType == OnboardingStepType.PolicyAcknowledgment && step.KnowledgeArticleId is { } articleId &&
                step.KnowledgeRevisionId is { } revisionId && !await repository.AcknowledgmentExistsAsync(step.Id, revisionId, cancellationToken))
                repository.Add(OnboardingAcknowledgment.Create(assignment.OrganizationId, assignment.Id, step.Id, assignment.EmployeeId,
                    articleId, revisionId, now));
        }
        var reading = await repository.GetReadingProgressesAsync(cycle.Id, cancellationToken);
        var requiredComplete = reading.Where(x => x.IsRequired).All(x => x.Status == OnboardingReadingStatus.Read);
        var wasLocked = assignment.AssessmentStatus == OnboardingAssessmentStatus.Locked;
        assignment.RefreshAssessmentAvailability(requiredComplete, now);
        var notifier = new OnboardingNotifier(notifications, repository);
        if (wasLocked && assignment.AssessmentStatus == OnboardingAssessmentStatus.Available) notifier.QuizUnlocked(assignment);
        var steps = await repository.GetStepsAsync(assignment.Id, cancellationToken);
        if (UpdateOnboardingStep.UpdateOnboardingStepCommandHandler.Refresh(assignment, steps, now))
            await notifier.JourneyCompletedAsync(assignment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetAssignmentDtoAsync(assignment.Id, cancellationToken) ?? throw new InvalidOperationException("Updated journey was not available.");
    }
}
