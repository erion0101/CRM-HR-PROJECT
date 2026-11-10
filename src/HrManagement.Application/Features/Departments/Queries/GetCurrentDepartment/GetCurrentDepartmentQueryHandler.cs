using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Departments.Queries.GetCurrentDepartment;

public sealed class GetCurrentDepartmentQueryHandler(IDepartmentRepository repository, ICurrentUserService currentUser)
    : IQueryHandler<GetCurrentDepartmentQuery, DepartmentOverviewDto>
{
    public async Task<DepartmentOverviewDto> Handle(GetCurrentDepartmentQuery request, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        return await repository.GetOverviewAsync(organizationId, request.DepartmentId, cancellationToken)
            ?? throw new NotFoundException("No active department has been configured.");
    }
}
