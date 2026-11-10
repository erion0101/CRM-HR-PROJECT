using HrManagement.Application.Common.Authorization;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Onboarding.Commands.UpdateOnboardingStep;

namespace HrManagement.Application.Features.Onboarding.Commands.ReviewOnboardingStep;

public sealed class ReviewOnboardingStepCommandHandler(IOnboardingRepository repository, IAuthorizedDataScope scope, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    : ICommandHandler<ReviewOnboardingStepCommand, OnboardingAssignmentDto>
{
    public async Task<OnboardingAssignmentDto> Handle(ReviewOnboardingStepCommand request, CancellationToken cancellationToken)
    {
        var assignment = await repository.GetAssignmentAsync(request.AssignmentId, cancellationToken) ?? throw new NotFoundException("Onboarding journey not found.");
        var responsible = currentUser.EmployeeId == assignment.ManagerEmployeeId || currentUser.EmployeeId == assignment.MentorEmployeeId ||
            currentUser.IsInRole(Roles.Hr);
        if (!responsible || !await scope.CanAccessEmployeeAsync(assignment.EmployeeId, cancellationToken)) throw new NotFoundException("Onboarding journey not found.");
        var step = await repository.GetStepAsync(assignment.Id, request.StepId, cancellationToken) ?? throw new NotFoundException("Onboarding step not found.");
        if (step.Version != request.ExpectedVersion) throw new ConflictException("The onboarding step was changed by another user.");
        step.Review(request.Approved, currentUser.UserId ?? throw new ForbiddenException(), request.ReviewNote, DateTimeOffset.UtcNow);
        var steps = await repository.GetStepsAsync(assignment.Id, cancellationToken);
        UpdateOnboardingStepCommandHandler.Refresh(assignment, steps, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetAssignmentDtoAsync(assignment.Id, cancellationToken) ?? throw new InvalidOperationException("Updated journey was not available.");
    }
}
