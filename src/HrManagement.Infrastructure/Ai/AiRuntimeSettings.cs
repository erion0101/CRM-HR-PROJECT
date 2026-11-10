using HrManagement.Application.Features.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace HrManagement.Infrastructure.Ai;

internal sealed class AiRuntimeSettings(IOptions<AiOptions> options, OpenAiCredentialResolver credentials) : IAiRuntimeSettings
{
    public AiOptions Value => options.Value;
    public bool ProviderConfigured => !string.IsNullOrWhiteSpace(credentials.GetApiKey());
    public bool UsagePricingConfigured => Value.InputCostPerMillionUsd > 0 && Value.OutputCostPerMillionUsd > 0;
}

internal sealed class OpenAiCredentialResolver(IConfiguration configuration)
{
    public string? GetApiKey()
    {
        var file = configuration["OPENAI_API_KEY_FILE"] ?? configuration["Ai:ApiKeyFile"];
        if (!string.IsNullOrWhiteSpace(file) && File.Exists(file)) return File.ReadAllText(file).Trim();
        return configuration["OPENAI_API_KEY"] ?? configuration["Ai:ApiKey"];
    }
}
