using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Onboarding.Commands.CreateOnboardingQuiz;

public sealed record CreateOnboardingQuizCommand(Guid TemplateId, string Title, int PassingPercentage = 80,
    int QuestionCount = 10, bool RandomizeQuestions = true, bool RandomizeOptions = true) : ICommand<OnboardingQuizAdminDto>;
