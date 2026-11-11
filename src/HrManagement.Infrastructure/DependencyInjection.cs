using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.AuditEvents;
using HrManagement.Application.Features.Ai;
using HrManagement.Application.Features.Compensation;
using HrManagement.Application.Features.Contracts;
using HrManagement.Application.Features.Departments;
using HrManagement.Application.Features.Documents;
using HrManagement.Application.Features.Employees;
using HrManagement.Application.Features.Positions;
using HrManagement.Application.Features.Users;
using HrManagement.Application.Features.Knowledge;
using HrManagement.Application.Features.Onboarding;
using HrManagement.Application.Features.Projects;
using HrManagement.Infrastructure.Auditing;
using HrManagement.Infrastructure.Authorization;
using HrManagement.Infrastructure.Ai;
using HrManagement.Infrastructure.Files;
using HrManagement.Infrastructure.Files.Options;
using HrManagement.Infrastructure.Health;
using HrManagement.Infrastructure.Identity;
using HrManagement.Infrastructure.Persistence.Interceptors;
using HrManagement.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;

namespace HrManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
        services.AddHttpContextAccessor();
        services.AddScoped<BackgroundJobContext>();
        services.AddScoped<ICurrentUserService, HttpCurrentUser>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddDbContext<AppDbContext>((provider, options) => options
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsql.UseVector();
            })
            .AddInterceptors(provider.GetRequiredService<AuditSaveChangesInterceptor>()));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IAuthorizedDataScope, AuthorizedDataScope>();

        services.AddStackExchangeRedisCache(options => options.Configuration = configuration.GetConnectionString("Redis") ?? "redis:6379");
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.Section));
        services.Configure<ClamAvOptions>(configuration.GetSection(ClamAvOptions.Section));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.Section));
        services.AddOptions<AiOptions>().Bind(configuration.GetSection(AiOptions.Section)).Validate(options =>
            options.EmbeddingDimensions == 1536 && options.MaxToolRounds is > 0 and <= 8 &&
            options.MaxToolCallsPerTurn is > 0 and <= 12 && options.ConversationRetentionDays is > 0 and <= 365 &&
            IsValidTimeZone(options.OrganizationTimeZone),
            "AI limits or embedding dimensions are invalid.").ValidateOnStart();

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();
        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "hr.session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
            options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
        });

        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<ICompensationRepository, CompensationRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IAuditEventRepository, AuditEventRepository>();
        services.AddScoped<IAiConversationRepository, AiConversationRepository>();
        services.AddScoped<IDocumentAiRepository, DocumentAiRepository>();
        services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IOnboardingRepository, OnboardingRepository>();
        services.AddScoped<HrManagement.Application.Features.Notifications.INotificationRepository, NotificationRepository>();
        services.AddScoped<HrManagement.Application.Features.Leave.ILeaveRepository, LeaveRepository>();
        services.AddScoped<IOnboardingOverdueReminderProcessor, OnboardingOverdueReminderProcessor>();
        services.AddScoped<HrManagement.Application.Features.Notifications.INotificationRetentionProcessor, NotificationRetentionProcessor>();
        services.AddScoped<HrManagement.Application.Features.Onboarding.Insights.IOnboardingInsightsReader, OnboardingInsightsReader>();
        services.AddScoped<IAiDataReader, AiDataReader>();
        services.AddScoped<IAiAuthorizationService, AiAuthorizationService>();
        services.AddScoped<IAiExecutionContextProvider, AiExecutionContextProvider>();
        services.AddScoped<IAiSourceAuthorizationService, AiSourceAuthorizationService>();
        services.AddScoped<IAiUsageService, AiUsageService>();
        services.AddSingleton<OpenAiCredentialResolver>();
        services.AddSingleton<IAiRuntimeSettings, AiRuntimeSettings>();
        services.AddSingleton<OpenAiProvider>();
        services.AddSingleton<IAiProvider>(provider => provider.GetRequiredService<OpenAiProvider>());
        services.AddSingleton<IEmbeddingProvider>(provider => provider.GetRequiredService<OpenAiProvider>());
        services.AddSingleton<IDocumentTextExtractor, LocalDocumentTextExtractor>();
        services.AddScoped<IDocumentIndexingProcessor, DocumentIndexingProcessor>();
        services.AddScoped<IAiMaintenanceProcessor, AiMaintenanceProcessor>();
        services.AddScoped<IOnboardingQuizGenerationProcessor, OnboardingQuizGenerationProcessor>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IDocumentProcessor, DocumentProcessor>();
        services.AddSingleton<IDocumentStorageService, LocalDocumentStorage>();
        services.AddSingleton<IDocumentScanner, ClamAvClient>();
        services.AddSingleton<IDocumentUploadPolicy, DocumentUploadPolicy>();
        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"])
            .AddCheck<RedisHealthCheck>("redis", tags: ["ready"])
            .AddCheck<StorageHealthCheck>("storage", tags: ["ready"])
            .AddCheck<ClamAvHealthCheck>("clamav", tags: ["ready"])
            .AddCheck<AiProviderHealthCheck>("ai-provider", tags: ["ai"]);
        return services;
    }

    private static bool IsValidTimeZone(string value)
    {
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(value); return true; }
        catch (TimeZoneNotFoundException) { return false; }
        catch (InvalidTimeZoneException) { return false; }
    }
}
