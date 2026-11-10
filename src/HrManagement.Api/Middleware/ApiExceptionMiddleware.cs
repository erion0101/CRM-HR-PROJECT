using HrManagement.Application.Common.Exceptions;
using HrManagement.Domain;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception)
        {
            var (status, title, detail, errors) = exception switch
            {
                ValidationException ex => (400, "Validation failed", ex.Message, ex.Errors),
                DomainException ex => (400, "Invalid operation", ex.Message, null),
                NotFoundException ex => (404, "Not found", ex.Message, null),
                ForbiddenException ex => (403, "Forbidden", ex.Message, null),
                ConflictException ex => (409, "Conflict", ex.Message, null),
                BadHttpRequestException ex => (400, "Invalid request", ex.Message, null),
                AntiforgeryValidationException ex => (400, "Invalid antiforgery token", ex.Message, null),
                _ => (500, "Unexpected error", "An unexpected error occurred.", null)
            };
            if (status == 500) logger.LogError(exception, "Unhandled error for correlation {CorrelationId}", context.TraceIdentifier);
            else logger.LogInformation(exception, "Request failed with {StatusCode}", status);
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetails { Status = status, Title = title, Detail = detail, Instance = context.Request.Path };
            problem.Extensions["correlationId"] = context.TraceIdentifier;
            if (errors is not null) problem.Extensions["errors"] = errors;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
