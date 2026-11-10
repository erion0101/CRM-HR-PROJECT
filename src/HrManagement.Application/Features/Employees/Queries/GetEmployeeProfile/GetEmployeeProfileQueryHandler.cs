using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Employees.Queries.GetEmployeeProfile;

public sealed class GetEmployeeProfileQueryHandler(IEmployeeRepository repository) : IQueryHandler<GetEmployeeProfileQuery, EmployeeProfileDto>
{
    public async Task<EmployeeProfileDto> Handle(GetEmployeeProfileQuery request, CancellationToken cancellationToken) =>
        await repository.GetProfileAccessibleAsync(request.EmployeeId, cancellationToken) ?? throw new NotFoundException("Employee not found.");
}
