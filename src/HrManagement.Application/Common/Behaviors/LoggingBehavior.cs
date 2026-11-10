using HrManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HrManagement.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger, ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName} for user {UserId} with correlation {CorrelationId}", requestName, currentUser.UserId, currentUser.CorrelationId);
        try
        {
            var response = await next();
            logger.LogInformation("Handled {RequestName}", requestName);
            return response;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Request {RequestName} failed", requestName);
            throw;
        }
    }
}
