using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.Departments.Queries.GetDepartments;

public sealed class GetDepartmentsQueryHandler(IDepartmentRepository repository, ICurrentUserService currentUser)
    : IQueryHandler<GetDepartmentsQuery, PageResult<DepartmentDto>>
{
    public async Task<PageResult<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var page = await repository.ListAsync(organizationId, request.Cursor, request.Limit, cancellationToken);
        return new PageResult<DepartmentDto>(page.Items.Select(x => new DepartmentDto(
            x.Department.Id,
            x.Department.Name,
            x.Department.Code,
            x.Department.Type,
            x.Department.ParentDepartmentId,
            x.Department.ManagerEmployeeId,
            x.ManagerName,
            x.Department.HrEmployeeId,
            x.HrName,
            x.Department.ProductManagerEmployeeId,
            x.ProductManagerName,
            x.Department.Description,
            x.DirectEmployeeCount,
            x.TotalEmployeeCount,
            x.ChildUnitCount,
            x.Department.Version)).ToArray(), page.NextCursor);
    }
}
