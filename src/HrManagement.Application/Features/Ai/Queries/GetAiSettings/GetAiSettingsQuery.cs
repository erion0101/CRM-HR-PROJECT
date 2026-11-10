using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Queries.GetAiSettings;

public sealed record GetAiSettingsQuery : IQuery<AiSettingsDto>;
