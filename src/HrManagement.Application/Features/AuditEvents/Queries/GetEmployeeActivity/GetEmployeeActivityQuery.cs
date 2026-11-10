using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.AuditEvents.Queries.GetEmployeeActivity;

public sealed record GetEmployeeActivityQuery(Guid EmployeeId, string? Cursor, int Limit = 30)
    : IQuery<PageResult<AuditEventDto>>;
