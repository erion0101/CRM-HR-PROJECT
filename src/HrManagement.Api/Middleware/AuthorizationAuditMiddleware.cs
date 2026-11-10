using HrManagement.Application.Common.Interfaces;

namespace HrManagement.Api.Middleware;

public sealed class AuthorizationAuditMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAuditWriter audit)
    {
        await next(context);
        if (context.Response.StatusCode == StatusCodes.Status403Forbidden && context.User.Identity?.IsAuthenticated == true)
        {
            try { await audit.RecordAccessAsync("PermissionDenied", "HttpRequest", $"{context.Request.Method} {context.Request.Path}", context.RequestAborted); }
            catch { /* An audit failure must not replace the denied response. */ }
        }
    }
}
