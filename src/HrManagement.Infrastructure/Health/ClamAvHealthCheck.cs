using System.Net.Sockets;
using HrManagement.Infrastructure.Files.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HrManagement.Infrastructure.Health;

internal sealed class ClamAvHealthCheck(IOptions<ClamAvOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(options.Value.Host, options.Value.Port, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) { return HealthCheckResult.Unhealthy("ClamAV is unavailable.", exception); }
    }
}
