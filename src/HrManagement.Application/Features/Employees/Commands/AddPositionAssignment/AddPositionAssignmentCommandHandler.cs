using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Departments;
using HrManagement.Application.Features.Positions;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Employees.Commands.AddPositionAssignment;

public sealed class AddPositionAssignmentCommandHandler(
    IEmployeeRepository employees,
    IPositionRepository positions,
    IDepartmentRepository departments,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : ICommandHandler<AddPositionAssignmentCommand, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(AddPositionAssignmentCommand request, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var employee = await employees.GetAccessibleAsync(request.EmployeeId, cancellationToken) ?? throw new NotFoundException("Employee not found.");
        var department = await departments.GetAsync(organizationId, request.DepartmentId, cancellationToken)
            ?? throw new ValidationException(new Dictionary<string, string[]> { ["departmentId"] = ["Department does not exist."] });
        if (!await positions.BelongsToDepartmentAsync(organizationId, request.PositionId, request.DepartmentId, cancellationToken))
            throw new ValidationException(new Dictionary<string, string[]> { ["positionId"] = ["Select a position that belongs to the selected department."] });

        var existing = await employees.GetAssignmentOnDateAsync(employee.Id, request.EffectiveFrom, cancellationToken);
        var correctedExisting = false;
        var assignmentUnchanged = false;
        if (existing is not null)
        {
            if (existing.PositionId == request.PositionId && existing.DepartmentId == request.DepartmentId)
                assignmentUnchanged = true;
            else if (request.EffectiveFrom < existing.EffectiveFrom)
                throw new ConflictException("The position change date cannot be before the current assignment start date.");
            else if (request.EffectiveFrom == existing.EffectiveFrom)
            {
                existing.Correct(request.PositionId, request.DepartmentId, DateTimeOffset.UtcNow);
                correctedExisting = true;
            }
            else
            {
                existing.End(request.EffectiveFrom.AddDays(-1), DateTimeOffset.UtcNow);
            }
        }
        if (!assignmentUnchanged && await employees.HasOverlappingAssignmentAsync(employee.Id, request.EffectiveFrom, request.EffectiveTo, existing?.Id, cancellationToken))
            throw new ConflictException("Position assignment overlaps another assignment.");
        if (!assignmentUnchanged && !correctedExisting)
            employees.AddAssignment(PositionAssignment.Create(organizationId, employee.Id, request.PositionId, request.DepartmentId, request.EffectiveFrom, request.EffectiveTo));

        if (department.ManagerEmployeeId is { } managerId && managerId != employee.Id)
        {
            var reportingLine = await employees.GetReportingLineOnDateAsync(employee.Id, request.EffectiveFrom, cancellationToken);
            if (reportingLine?.ManagerEmployeeId != managerId)
            {
                var correctedReportingLine = false;
                if (reportingLine is not null)
                {
                    if (request.EffectiveFrom == reportingLine.EffectiveFrom)
                    {
                        reportingLine.Correct(managerId, DateTimeOffset.UtcNow);
                        correctedReportingLine = true;
                    }
                    else if (request.EffectiveFrom > reportingLine.EffectiveFrom)
                    {
                        reportingLine.End(request.EffectiveFrom.AddDays(-1), DateTimeOffset.UtcNow);
                    }
                }
                if (await employees.HasOverlappingReportingLineAsync(employee.Id, request.EffectiveFrom, request.EffectiveTo, reportingLine?.Id, cancellationToken))
                    throw new ConflictException("The department manager assignment overlaps another reporting line.");
                if (!correctedReportingLine)
                    employees.AddReportingLine(ReportingLine.Create(organizationId, employee.Id, managerId, request.EffectiveFrom, request.EffectiveTo));
            }
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await employees.GetDetailsAsync(employee, cancellationToken)).ToDto();
    }
}
