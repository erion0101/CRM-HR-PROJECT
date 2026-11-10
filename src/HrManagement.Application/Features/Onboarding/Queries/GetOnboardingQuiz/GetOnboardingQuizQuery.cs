using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Queries.GetOnboardingQuiz;

public sealed record GetOnboardingQuizQuery(Guid TemplateId) : IQuery<OnboardingQuizAdminDto?>;
