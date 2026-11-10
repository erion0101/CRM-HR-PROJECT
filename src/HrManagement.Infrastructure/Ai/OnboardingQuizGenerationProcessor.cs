using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Features.Ai;
using HrManagement.Application.Features.Onboarding;
using HrManagement.Application.Features.Onboarding.Commands.RunOnboardingQuizGeneration;
using HrManagement.Domain;
using HrManagement.Infrastructure.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HrManagement.Infrastructure.Ai;

/// <summary>
/// Picks up queued (or stalled) quiz-generation jobs across organizations, leases each one, and runs the
/// generation use case in a fresh DI scope on behalf of the HR user who queued it.
/// </summary>
internal sealed class OnboardingQuizGenerationProcessor(
    AppDbContext db,
    IServiceScopeFactory scopeFactory,
    IAiRuntimeSettings settings,
    ILogger<OnboardingQuizGenerationProcessor> logger) : IOnboardingQuizGenerationProcessor
{
    private const int BatchSize = 3;

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var jobs = await db.OnboardingQuizzes
            .Where(x => x.GenerationStatus == OnboardingQuizGenerationStatus.Queued ||
                (x.GenerationStatus == OnboardingQuizGenerationStatus.Running && x.GenerationLeaseUntilUtc < now))
            .OrderBy(x => x.UpdatedAtUtc).Take(BatchSize).ToListAsync(cancellationToken);
        if (jobs.Count == 0) return 0;

        var lease = TimeSpan.FromSeconds(settings.Value.BackgroundRequestTimeoutSeconds + 60);
        var processed = 0;
        foreach (var job in jobs)
        {
            if (!settings.Value.Enabled || !settings.Value.RealDataApproved || !settings.ProviderConfigured)
            {
                job.FailGeneration("AI was disabled before generation started. Enable AI or add questions manually.", DateTimeOffset.UtcNow);
                await db.SaveChangesAsync(cancellationToken);
                processed++;
                continue;
            }
            var leased = job.TryLeaseGeneration(DateTimeOffset.UtcNow, lease);
            try
            {
                // Saving the lease also guards against two workers taking the same job (optimistic concurrency).
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                continue;
            }
            processed++;
            if (!leased) continue;

            try
            {
                await RunAsync(job, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Onboarding quiz generation failed for quiz {QuizId}.", job.Id);
                await RecordFailureAsync(job, Describe(exception), cancellationToken);
            }
        }
        return processed;
    }

    private async Task RunAsync(OnboardingQuiz job, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<BackgroundJobContext>()
            .RunAs(job.OrganizationId, job.GenerationRequestedByUserId, $"quiz-generation:{job.Id}");
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(new RunOnboardingQuizGenerationCommand(job.Id), cancellationToken);
    }

    /// <summary>Uses a new scope so nothing the failed attempt added to its change tracker is saved.</summary>
    private async Task RecordFailureAsync(OnboardingQuiz job, string reason, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var quiz = await context.OnboardingQuizzes.SingleOrDefaultAsync(x => x.Id == job.Id, cancellationToken);
        if (quiz is null || quiz.GenerationStatus != OnboardingQuizGenerationStatus.Running) return;
        quiz.FailGeneration(reason, DateTimeOffset.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Only application errors carry user-facing text; anything else gets a generic message.</summary>
    private static string Describe(Exception exception) => exception switch
    {
        ValidationException validation => validation.Errors.Values.SelectMany(x => x).FirstOrDefault() ?? "AI generation failed validation.",
        ConflictException or NotFoundException or DomainException => exception.Message,
        OperationCanceledException => "The AI provider did not respond in time. Try again.",
        _ => "AI generation failed unexpectedly. Try again or add questions manually."
    };
}
