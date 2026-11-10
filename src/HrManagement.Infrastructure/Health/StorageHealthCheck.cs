using HrManagement.Infrastructure.Files.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HrManagement.Infrastructure.Health;

internal sealed class StorageHealthCheck(IOptions<StorageOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(options.Value.RootPath);
            var path = Path.Combine(options.Value.RootPath, ".health");
            File.WriteAllText(path, "ok");
            File.Delete(path);
            return Task.FromResult(HealthCheckResult.Healthy());
        }
        catch (Exception exception) { return Task.FromResult(HealthCheckResult.Unhealthy("Document storage is unavailable.", exception)); }
    }
}
