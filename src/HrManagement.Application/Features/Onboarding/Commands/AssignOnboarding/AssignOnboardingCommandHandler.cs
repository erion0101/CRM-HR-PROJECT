using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Notifications;
using HrManagement.Application.Features.Projects;

namespace HrManagement.Application.Features.Onboarding.Commands.AssignOnboarding;

/// <summary>
/// HR or a manager assigns an onboarding journey to an employee in their scope. When a company package is assigned
/// with a project, the project's published package is included automatically (see <see cref="OnboardingJourneyFactory"/>).
/// </summary>
public sealed class AssignOnboardingCommandHandler(
    IOnboardingRepository repository,
    IProjectRepository projects,
    IAuthorizedDataScope scope,
    INotificationRepository notifications,
    IUnitOfWork unitOfWork) : ICommandHandler<AssignOnboardingCommand, OnboardingAssignmentDto>
{
    public async Task<OnboardingAssignmentDto> Handle(AssignOnboardingCommand request, CancellationToken cancellationToken)
    {
        if (!await scope.CanAccessEmployeeAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        var assignment = await new OnboardingJourneyFactory(repository, projects, notifications).CreateAsync(scope.OrganizationId,
            new OnboardingJourneyRequest(request.EmployeeId, request.TemplateId, request.ProjectId, request.ManagerEmployeeId,
                request.MentorEmployeeId, request.StartDate, request.ReadingStepIds), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetAssignmentDtoAsync(assignment.Id, cancellationToken) ?? throw new InvalidOperationException("Assigned onboarding journey was not available.");
    }
}
