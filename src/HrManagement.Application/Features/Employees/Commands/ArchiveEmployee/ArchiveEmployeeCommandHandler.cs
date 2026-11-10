using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Employees.Commands.ArchiveEmployee;

public sealed class ArchiveEmployeeCommandHandler(IEmployeeRepository repository, IUnitOfWork unitOfWork) : ICommandHandler<ArchiveEmployeeCommand>
{
    public async Task Handle(ArchiveEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await repository.GetAccessibleAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Employee not found.");
        if (employee.Version != request.ExpectedVersion) throw new ConflictException("The record was changed by another user.");
        employee.Archive(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
