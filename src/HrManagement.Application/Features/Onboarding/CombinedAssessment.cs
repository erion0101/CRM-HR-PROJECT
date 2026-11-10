using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding;

/// <summary>
/// Rules for the single end-of-journey quiz when a journey combines a company and a project package.
/// Each package keeps its own question bank; one attempt draws each bank's per-attempt count,
/// and the stricter passing percentage applies to the combined score.
/// </summary>
public static class CombinedAssessment
{
    public static int PassingPercentage(IReadOnlyCollection<OnboardingQuiz> quizzes) =>
        quizzes.Count == 0 ? 0 : quizzes.Max(x => x.PassingPercentage);

    public static int QuestionCount(IReadOnlyCollection<OnboardingQuiz> quizzes) => quizzes.Sum(x => x.QuestionCount);
}
