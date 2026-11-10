using HrManagement.Application.Common.Interfaces;

namespace HrManagement.Worker;

public sealed class DocumentProcessingWorker(IServiceScopeFactory scopeFactory, ILogger<DocumentProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var count = await scope.ServiceProvider.GetRequiredService<IDocumentProcessor>().ProcessPendingAsync(stoppingToken);
                count += await scope.ServiceProvider.GetRequiredService<HrManagement.Application.Features.Ai.IDocumentIndexingProcessor>().ProcessPendingAsync(stoppingToken);
                count += await scope.ServiceProvider.GetRequiredService<HrManagement.Application.Features.Ai.IAiMaintenanceProcessor>().PurgeExpiredConversationsAsync(stoppingToken);
                count += await scope.ServiceProvider.GetRequiredService<HrManagement.Application.Features.Onboarding.IOnboardingQuizGenerationProcessor>().ProcessPendingAsync(stoppingToken);
                count += await scope.ServiceProvider.GetRequiredService<HrManagement.Application.Features.Onboarding.IOnboardingOverdueReminderProcessor>().ProcessAsync(stoppingToken);
                await Task.Delay(count == 0 ? TimeSpan.FromSeconds(5) : TimeSpan.FromMilliseconds(250), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogError(exception, "Document processing iteration failed.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
