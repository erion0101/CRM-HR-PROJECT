using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Employees.Events;
using HrManagement.Domain;
using MediatR;

namespace HrManagement.Application.Features.Employees.Commands.SetEmployment;

public sealed class SetEmploymentCommandHandler(IEmployeeRepository repository, IUnitOfWork unitOfWork, ICurrentUserService currentUser,
    IPublisher publisher)
    : ICommandHandler<SetEmploymentCommand, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(SetEmploymentCommand request, CancellationToken cancellationToken)
    {
        var employee = await repository.GetAccessibleAsync(request.EmployeeId, cancellationToken) ?? throw new NotFoundException("Employee not found.");
        var employment = await repository.GetOpenEmploymentAsync(employee.Id, cancellationToken);
        var wasActive = employment?.Status == EmploymentStatus.Active;
        if (employment is null)
            repository.AddEmployment(Employment.Create(currentUser.OrganizationId ?? throw new ForbiddenException(), employee.Id, request.Status, request.StartDate, request.EndDate));
        else
            employment.Change(request.Status, request.StartDate, request.EndDate, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        // Published after the employment is saved, so a subscriber's failure cannot roll it back.
        if (!wasActive && request.Status == EmploymentStatus.Active)
            await publisher.Publish(new EmploymentActivatedNotification(employee.Id, request.StartDate), cancellationToken);
        return (await repository.GetDetailsAsync(employee, cancellationToken)).ToDto();
    }
}
