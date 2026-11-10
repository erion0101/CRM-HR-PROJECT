using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommandHandler(IEmployeeRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateEmployeeCommand, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await repository.GetAccessibleAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Employee not found.");
        if (employee.Version != request.ExpectedVersion) throw new ConflictException("The record was changed by another user.");
        employee.Update(request.FirstName, request.LastName, request.WorkEmail, request.Phone, request.DateOfBirth, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await repository.GetDetailsAsync(employee, cancellationToken)).ToDto();
    }
}
