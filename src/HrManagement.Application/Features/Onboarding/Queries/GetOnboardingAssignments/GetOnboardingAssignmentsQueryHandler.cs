using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Queries.GetOnboardingAssignments;

public sealed class GetOnboardingAssignmentsQueryHandler(IOnboardingRepository repository)
    : IQueryHandler<GetOnboardingAssignmentsQuery, IReadOnlyList<OnboardingAssignmentDto>>
{
    public Task<IReadOnlyList<OnboardingAssignmentDto>> Handle(GetOnboardingAssignmentsQuery request, CancellationToken cancellationToken) =>
        repository.ListAccessibleAssignmentsAsync(request.EmployeeId, cancellationToken);
}
