using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Employees.Queries.GetEmployeeById;

public sealed class GetEmployeeByIdQueryHandler(IEmployeeRepository repository) : IQueryHandler<GetEmployeeByIdQuery, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
    {
        var employee = await repository.GetAccessibleAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Employee not found.");
        return (await repository.GetDetailsAsync(employee, cancellationToken)).ToDto();
    }
}
