using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Queries.GetQuizAttempt;

public sealed class GetQuizAttemptQueryHandler(IOnboardingRepository repository, ICurrentUserService currentUser)
    : IQueryHandler<GetQuizAttemptQuery, OnboardingQuizAttemptDto>
{
    public async Task<OnboardingQuizAttemptDto> Handle(GetQuizAttemptQuery request, CancellationToken cancellationToken)
    {
        var attempt = await repository.GetAttemptAsync(request.AttemptId, cancellationToken) ?? throw new NotFoundException("Quiz attempt not found.");
        if (currentUser.EmployeeId != attempt.EmployeeId) throw new NotFoundException("Quiz attempt not found.");
        return await repository.GetAttemptDtoAsync(attempt.Id, true, cancellationToken) ?? throw new NotFoundException("Quiz attempt not found.");
    }
}
