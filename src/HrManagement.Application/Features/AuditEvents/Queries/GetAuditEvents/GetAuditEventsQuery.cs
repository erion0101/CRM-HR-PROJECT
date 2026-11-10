using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.AuditEvents.Queries.GetAuditEvents;

public sealed record GetAuditEventsQuery(string? EntityType, Guid? UserId, DateTimeOffset? From, string? Cursor, int Limit = 50) : IQuery<PageResult<AuditEventDto>>;
