using HrManagement.Application.Common.Authorization;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;
using HrManagement.Application.Features.Employees;

namespace HrManagement.Application.Features.AuditEvents.Queries.GetEmployeeActivity;

public sealed class GetEmployeeActivityQueryHandler(
    IAuditEventRepository auditEvents,
    IEmployeeRepository employees,
    ICurrentUserService currentUser) : IQueryHandler<GetEmployeeActivityQuery, PageResult<AuditEventDto>>
{
    public async Task<PageResult<AuditEventDto>> Handle(GetEmployeeActivityQuery request, CancellationToken cancellationToken)
    {
        if (!await employees.ExistsAccessibleAsync(request.EmployeeId, cancellationToken))
            throw new NotFoundException("Employee not found.");
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var includeCompensation = currentUser.IsInRole(Roles.Hr);
        return await auditEvents.SearchEmployeeActivityAsync(organizationId, request.EmployeeId, includeCompensation, request.Cursor, request.Limit, cancellationToken);
    }
}
