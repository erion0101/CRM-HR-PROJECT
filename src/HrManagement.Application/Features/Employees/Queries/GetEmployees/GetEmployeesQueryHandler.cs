using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.Employees.Queries.GetEmployees;

public sealed class GetEmployeesQueryHandler(IEmployeeRepository repository) : IQueryHandler<GetEmployeesQuery, PageResult<EmployeeDto>>
{
    public async Task<PageResult<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var page = await repository.SearchAccessibleAsync(request.Search, request.DepartmentId, request.Status, request.Cursor, request.Limit, cancellationToken);
        return new PageResult<EmployeeDto>(page.Items.Select(x => x.ToDto()).ToArray(), page.NextCursor);
    }
}
