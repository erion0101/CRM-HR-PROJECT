using HrManagement.Application.Common.Authorization;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Ai;
using HrManagement.Domain;

namespace HrManagement.Infrastructure.Ai;

internal sealed class AiAuthorizationService(
    IAiRuntimeSettings settings,
    ICurrentUserService currentUser,
    IAuthorizedDataScope scope) : IAiAuthorizationService
{
    public Task<AiCapabilitiesDto> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        var value = settings.Value;
        var capabilities = new List<string> { "AI.Use", "AI.Organization.Read", "AI.Employee.Read", "AI.Contracts.Read", "AI.Documents.Read", "AI.Knowledge.Read", "AI.Projects.Read", "AI.Onboarding.Read" };
        if (Roles.All.Any(currentUser.IsInRole))
            capabilities.Add("AI.EmployeePrivate.Read");
        if (currentUser.IsInRole(Roles.Hr)) capabilities.Add("AI.Settings.Read/Test");
        var enabled = value.Enabled && value.RealDataApproved && settings.ProviderConfigured && settings.UsagePricingConfigured;
        var reason = !value.Enabled ? "AI is disabled by configuration."
            : !value.RealDataApproved ? "Real-data AI use has not passed the governance gate."
            : !settings.ProviderConfigured ? "The AI provider credential is not configured."
            : !settings.UsagePricingConfigured ? "AI model pricing is not configured, so budget enforcement cannot run safely."
            : null;
        return Task.FromResult(new AiCapabilitiesDto(enabled, value.RealDataApproved, settings.ProviderConfigured,
            capabilities, enabled ? SuggestedPrompts() : [], reason));
    }

    public async Task EnsureDocumentApprovalAllowedAsync(Guid employeeId, DataClassification classification, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(Roles.Hr)) throw new ForbiddenException();
        if (classification is DataClassification.Sensitive or DataClassification.HighlyRestricted)
            throw new ValidationException(new Dictionary<string, string[]> { ["classification"] = ["Sensitive and Highly Restricted documents cannot be indexed by AI."] });
        if (!await scope.CanAccessEmployeeAsync(employeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
    }

    private static IReadOnlyList<string> SuggestedPrompts() =>
    [
        "Show the employees I am allowed to see.",
        "Which accessible contracts expire in the next 90 days?",
        "Show my organization structure."
        ,"What should I complete next in my onboarding?"
        ,"Explain one of my assigned projects."
        ,"Search company knowledge for getting started guidance."
    ];
}
