using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Queries.GetAiCapabilities;

public sealed record GetAiCapabilitiesQuery : IQuery<AiCapabilitiesDto>;
