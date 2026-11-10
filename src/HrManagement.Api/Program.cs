using HrManagement.Api.Configuration;
using HrManagement.Api.Middleware;
using HrManagement.Application;
using HrManagement.Infrastructure;
using HrManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
var keyPath = Path.Combine(builder.Configuration["Storage:RootPath"] ?? "/data", "keys");
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keyPath)).SetApplicationName("HrManagement");
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi();

var app = builder.Build();
if (args.Contains("--migrate-only", StringComparer.Ordinal))
{
    await app.Services.InitializeDatabaseAsync();
    return;
}

app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
app.UseAuthentication();
app.UseMiddleware<AuthorizationAuditMiddleware>();
app.UseAuthorization();
app.UseAntiforgery();
app.UseMiddleware<AuthenticatedAntiforgeryMiddleware>();

if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();
app.MapHealthChecks("/health/ai", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ai") }).RequireAuthorization(Policies.AiAdmin);
app.MapControllers();
await app.RunAsync();

public partial class Program;
