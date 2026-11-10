using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandHandler(IEmployeeRepository repository, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : ICommandHandler<CreateEmployeeCommand, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = Employee.Create(currentUser.OrganizationId ?? throw new ForbiddenException(), request.EmployeeNumber, request.FirstName, request.LastName, request.WorkEmail, request.Phone, request.DateOfBirth);
        repository.Add(employee);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await repository.GetDetailsAsync(employee, cancellationToken)).ToDto();
    }
}
