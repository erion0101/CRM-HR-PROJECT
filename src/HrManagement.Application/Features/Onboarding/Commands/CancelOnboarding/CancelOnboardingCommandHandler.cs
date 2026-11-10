using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.CancelOnboarding;

public sealed class CancelOnboardingCommandHandler(IOnboardingRepository repository, IAuthorizedDataScope scope, IUnitOfWork unitOfWork)
    : ICommandHandler<CancelOnboardingCommand, OnboardingAssignmentDto>
{
    public async Task<OnboardingAssignmentDto> Handle(CancelOnboardingCommand request, CancellationToken cancellationToken)
    {
        var assignment = await repository.GetAssignmentAsync(request.AssignmentId, cancellationToken) ?? throw new NotFoundException("Onboarding journey not found.");
        if (!await scope.CanAccessEmployeeAsync(assignment.EmployeeId, cancellationToken)) throw new NotFoundException("Onboarding journey not found.");
        if (assignment.Version != request.ExpectedVersion) throw new ConflictException("The onboarding journey was changed by another user.");
        assignment.Cancel(request.Reason, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetAssignmentDtoAsync(assignment.Id, cancellationToken) ?? throw new InvalidOperationException("Cancelled journey was not available.");
    }
}
