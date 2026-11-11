using System.Text.Json.Serialization;
using HrManagement.Application.Common.Authorization;

namespace HrManagement.Api.Configuration;

public static class ApiDependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddOpenApi();
        services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "hr.antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Admin, policy => policy.RequireRole(Roles.Hr))
            .AddPolicy(Policies.OrganizationWrite, policy => policy.RequireRole(Roles.Hr))
            .AddPolicy(Policies.PeopleRead, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(Policies.PeopleWrite, policy => policy.RequireRole(Roles.Hr))
            .AddPolicy(Policies.ContractsWrite, policy => policy.RequireRole(Roles.Hr))
            .AddPolicy(Policies.Salary, policy => policy.RequireRole(Roles.Hr))
            .AddPolicy(Policies.DocumentsRead, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(Policies.DocumentsUpload, policy => policy.RequireAssertion(context =>
            {
                if (context.User.IsInRole(Roles.Hr))
                    return true;
                if (context.Resource is not HttpContext httpContext ||
                    !Guid.TryParse(context.User.FindFirst("employee_id")?.Value, out var currentEmployeeId) ||
                    !Guid.TryParse(httpContext.Request.RouteValues["employeeId"]?.ToString(), out var requestedEmployeeId))
                    return false;
                return currentEmployeeId == requestedEmployeeId;
            }))
            .AddPolicy(Policies.DocumentsWrite, policy => policy.RequireRole(Roles.Hr))
            .AddPolicy(Policies.Audit, policy => policy.RequireRole(Roles.Hr))
            .AddPolicy(Policies.AiUse, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(Policies.AiAdmin, policy => policy.RequireRole(Roles.Hr))
            .AddPolicy(Policies.KnowledgeManage, policy => policy.RequireRole(Roles.Hr, Roles.ProductManager))
            .AddPolicy(Policies.OnboardingAdmin, policy => policy.RequireRole(Roles.Hr))
            .AddPolicy(Policies.OnboardingManage, policy => policy.RequireRole(Roles.Hr, Roles.Manager))
            .AddPolicy(Policies.ProjectCreate, policy => policy.RequireRole(Roles.Hr, Roles.ProductManager))
            .AddPolicy(Policies.LeaveAdmin, policy => policy.RequireRole(Roles.Hr))
            .AddPolicy(Policies.LeaveApprove, policy => policy.RequireRole(Roles.Hr, Roles.Manager));
        return services;
    }
}
