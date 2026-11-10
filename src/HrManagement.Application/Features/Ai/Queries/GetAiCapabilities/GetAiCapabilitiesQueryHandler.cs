using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Queries.GetAiCapabilities;

public sealed class GetAiCapabilitiesQueryHandler(IAiAuthorizationService authorization) : IQueryHandler<GetAiCapabilitiesQuery, AiCapabilitiesDto>
{
    public Task<AiCapabilitiesDto> Handle(GetAiCapabilitiesQuery request, CancellationToken cancellationToken) => authorization.GetCapabilitiesAsync(cancellationToken);
}
