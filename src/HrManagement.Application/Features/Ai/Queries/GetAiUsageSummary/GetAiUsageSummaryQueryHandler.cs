using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Queries.GetAiUsageSummary;

public sealed class GetAiUsageSummaryQueryHandler(IAiConversationRepository repository) : IQueryHandler<GetAiUsageSummaryQuery, AiUsageSummaryDto>
{
    public Task<AiUsageSummaryDto> Handle(GetAiUsageSummaryQuery request, CancellationToken cancellationToken) => repository.GetUsageSummaryAsync(cancellationToken);
}
