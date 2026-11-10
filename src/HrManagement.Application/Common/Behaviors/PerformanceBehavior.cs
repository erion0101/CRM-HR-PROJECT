using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HrManagement.Application.Common.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private const long SlowRequestMilliseconds = 500;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try { return await next(); }
        finally
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds >= SlowRequestMilliseconds)
                logger.LogWarning("Long-running request {RequestName} took {ElapsedMilliseconds} ms", typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);
        }
    }
}
