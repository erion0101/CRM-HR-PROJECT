using HrManagement.Application.Features.Ai;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HrManagement.Infrastructure.Health;

internal sealed class AiProviderHealthCheck(IAiRuntimeSettings settings, IAiProvider provider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!settings.Value.Enabled) return HealthCheckResult.Healthy("AI is disabled.");
        if (!settings.Value.RealDataApproved) return HealthCheckResult.Degraded("AI governance approval is incomplete.");
        if (!settings.ProviderConfigured) return HealthCheckResult.Unhealthy("AI provider credential is missing.");
        if (!settings.UsagePricingConfigured) return HealthCheckResult.Degraded("AI model pricing is missing; budget enforcement is not ready.");
        try { return await provider.TestConnectionAsync(cancellationToken) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy(); }
        catch (Exception exception) when (exception is not OperationCanceledException) { return HealthCheckResult.Unhealthy("AI provider connection failed.", exception); }
    }
}
