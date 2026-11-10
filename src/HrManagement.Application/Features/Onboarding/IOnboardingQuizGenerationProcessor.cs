namespace HrManagement.Application.Features.Onboarding;

/// <summary>Background processing of queued AI quiz-generation jobs (runs in the worker).</summary>
public interface IOnboardingQuizGenerationProcessor
{
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken);
}
