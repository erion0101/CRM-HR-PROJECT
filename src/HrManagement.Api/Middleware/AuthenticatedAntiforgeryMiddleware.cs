using Microsoft.AspNetCore.Antiforgery;

namespace HrManagement.Api.Middleware;

public sealed class AuthenticatedAntiforgeryMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> UnsafeMethods = new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        var exempt = context.Request.Path.StartsWithSegments("/api/v1/auth/login") || context.Request.Path.StartsWithSegments("/health");
        if (!exempt && context.User.Identity?.IsAuthenticated == true && UnsafeMethods.Contains(context.Request.Method)) await antiforgery.ValidateRequestAsync(context);
        await next(context);
    }
}
