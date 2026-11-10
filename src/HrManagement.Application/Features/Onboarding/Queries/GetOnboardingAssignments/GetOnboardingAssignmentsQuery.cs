using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Queries.GetOnboardingAssignments;

public sealed record GetOnboardingAssignmentsQuery(Guid? EmployeeId) : IQuery<IReadOnlyList<OnboardingAssignmentDto>>;
