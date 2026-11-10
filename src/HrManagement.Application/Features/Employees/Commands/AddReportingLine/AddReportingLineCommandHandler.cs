using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Departments;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Employees.Commands.AddReportingLine;

public sealed class AddReportingLineCommandHandler(IEmployeeRepository repository, IDepartmentRepository departments, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : ICommandHandler<AddReportingLineCommand, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(AddReportingLineCommand request, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var employee = await repository.GetAccessibleAsync(request.EmployeeId, cancellationToken) ?? throw new NotFoundException("Employee not found.");
        var assignment = await repository.GetAssignmentOnDateAsync(employee.Id, request.EffectiveFrom, cancellationToken)
            ?? throw new ValidationException(new Dictionary<string, string[]> { ["department"] = ["Assign the employee to a department before applying a manager."] });
        var department = await departments.GetAsync(organizationId, assignment.DepartmentId, cancellationToken)
            ?? throw new NotFoundException("Department not found.");
        var managerId = department.ManagerEmployeeId
            ?? throw new ValidationException(new Dictionary<string, string[]> { ["manager"] = ["Configure a manager for this department first."] });
        if (managerId == employee.Id)
            throw new ConflictException("This employee is the department manager and cannot report to themselves.");
        if (!await repository.ExistsAccessibleAsync(managerId, cancellationToken))
            throw new ValidationException(new Dictionary<string, string[]> { ["manager"] = ["The department manager is outside your accessible employee scope."] });

        var existing = await repository.GetReportingLineOnDateAsync(employee.Id, request.EffectiveFrom, cancellationToken);
        if (existing?.ManagerEmployeeId == managerId)
            return (await repository.GetDetailsAsync(employee, cancellationToken)).ToDto();

        var correctedExisting = false;
        if (existing is not null)
        {
            if (request.EffectiveFrom < existing.EffectiveFrom)
                throw new ConflictException("The manager change date cannot be before the current reporting line start date.");
            if (request.EffectiveFrom == existing.EffectiveFrom)
            {
                existing.Correct(managerId, DateTimeOffset.UtcNow);
                correctedExisting = true;
            }
            else
            {
                existing.End(request.EffectiveFrom.AddDays(-1), DateTimeOffset.UtcNow);
            }
        }
        if (await repository.HasOverlappingReportingLineAsync(employee.Id, request.EffectiveFrom, request.EffectiveTo, existing?.Id, cancellationToken))
            throw new ConflictException("Manager assignment overlaps another reporting line.");
        if (!correctedExisting)
            repository.AddReportingLine(ReportingLine.Create(organizationId, employee.Id, managerId, request.EffectiveFrom, request.EffectiveTo));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await repository.GetDetailsAsync(employee, cancellationToken)).ToDto();
    }
}
