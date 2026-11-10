using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.AuditEvents.Queries.GetAuditEvents;

public sealed class GetAuditEventsQueryHandler(IAuditEventRepository repository, ICurrentUserService currentUser)
    : IQueryHandler<GetAuditEventsQuery, PageResult<AuditEventDto>>
{
    public Task<PageResult<AuditEventDto>> Handle(GetAuditEventsQuery request, CancellationToken cancellationToken) =>
        repository.SearchAsync(currentUser.OrganizationId ?? throw new ForbiddenException(), request.EntityType, request.UserId, request.From, request.Cursor, request.Limit, cancellationToken);
}
