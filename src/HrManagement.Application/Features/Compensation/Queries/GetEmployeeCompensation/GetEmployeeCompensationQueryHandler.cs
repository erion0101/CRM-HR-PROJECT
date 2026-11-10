using HrManagement.Application.Common.Authorization;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Employees;

namespace HrManagement.Application.Features.Compensation.Queries.GetEmployeeCompensation;

public sealed class GetEmployeeCompensationQueryHandler(ICompensationRepository compensation, IEmployeeRepository employees, ICurrentUserService currentUser)
    : IQueryHandler<GetEmployeeCompensationQuery, IReadOnlyList<CompensationDto>>
{
    public async Task<IReadOnlyList<CompensationDto>> Handle(GetEmployeeCompensationQuery request, CancellationToken cancellationToken)
    {
        EnsureSalaryAccess();
        if (!await employees.ExistsAccessibleAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        return (await compensation.ListAsync(request.EmployeeId, cancellationToken)).Select(x => x.ToDto()).ToArray();
    }

    private void EnsureSalaryAccess()
    {
        if (!currentUser.IsInRole(Roles.Hr)) throw new ForbiddenException();
    }
}
