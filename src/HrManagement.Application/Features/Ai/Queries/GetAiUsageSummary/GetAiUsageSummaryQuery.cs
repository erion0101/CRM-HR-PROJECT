using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Queries.GetAiUsageSummary;

public sealed record GetAiUsageSummaryQuery : IQuery<AiUsageSummaryDto>;
