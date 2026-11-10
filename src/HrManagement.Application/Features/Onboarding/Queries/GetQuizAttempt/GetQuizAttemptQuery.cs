using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Queries.GetQuizAttempt;

public sealed record GetQuizAttemptQuery(Guid AttemptId) : IQuery<OnboardingQuizAttemptDto>;
