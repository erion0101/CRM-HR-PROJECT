using FluentValidation;
using HrManagement.Application.Common.Behaviors;
using HrManagement.Application.Features.Ai;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HrManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IAiToolRegistry, AiToolRegistry>();
        services.AddScoped<IAiOrchestrator, AiOrchestrator>();
        return services;
    }
}
