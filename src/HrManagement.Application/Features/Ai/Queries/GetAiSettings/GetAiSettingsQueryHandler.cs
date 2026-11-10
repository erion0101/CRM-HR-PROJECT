using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Queries.GetAiSettings;

public sealed class GetAiSettingsQueryHandler(IAiRuntimeSettings settings) : IQueryHandler<GetAiSettingsQuery, AiSettingsDto>
{
    public Task<AiSettingsDto> Handle(GetAiSettingsQuery request, CancellationToken cancellationToken)
    {
        var x = settings.Value;
        return Task.FromResult(new AiSettingsDto(x.Enabled, x.RealDataApproved, settings.ProviderConfigured, x.Provider, x.ChatModel,
            x.EmbeddingModel, x.ConversationRetentionDays, x.RequestsPerDayPerUser, x.RequestsPerDayPerOrganization, x.MonthlyBudgetUsd));
    }
}
