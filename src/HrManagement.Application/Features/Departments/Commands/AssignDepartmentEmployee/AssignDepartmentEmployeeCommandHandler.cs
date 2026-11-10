using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Employees;
using HrManagement.Application.Features.Positions;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Departments.Commands.AssignDepartmentEmployee;

public sealed class AssignDepartmentEmployeeCommandHandler(
    IDepartmentRepository departments,
    IEmployeeRepository employees,
    IPositionRepository positions,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : ICommandHandler<AssignDepartmentEmployeeCommand, DepartmentOverviewDto>
{
    public async Task<DepartmentOverviewDto> Handle(AssignDepartmentEmployeeCommand request, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        _ = await departments.GetAsync(organizationId, request.DepartmentId, cancellationToken)
            ?? throw new NotFoundException("Department not found.");
        var employee = await employees.GetAccessibleAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee not found.");
        if (!await positions.BelongsToDepartmentAsync(organizationId, request.PositionId, request.DepartmentId, cancellationToken))
            throw new ValidationException(new Dictionary<string, string[]> { ["positionId"] = ["Select a position that belongs to this department."] });

        var existing = await employees.GetAssignmentOnDateAsync(employee.Id, request.EffectiveFrom, cancellationToken);
        if (existing is not null)
        {
            if (existing.DepartmentId == request.DepartmentId && existing.PositionId == request.PositionId)
                throw new ConflictException("The employee already has this department and position assignment.");
            if (request.EffectiveFrom <= existing.EffectiveFrom)
                throw new ConflictException("The transfer date must be after the current assignment start date.");
            existing.End(request.EffectiveFrom.AddDays(-1), DateTimeOffset.UtcNow);
        }

        if (await employees.HasOverlappingAssignmentAsync(employee.Id, request.EffectiveFrom, null, existing?.Id, cancellationToken))
            throw new ConflictException("The new department assignment overlaps another assignment.");

        employees.AddAssignment(PositionAssignment.Create(organizationId, employee.Id, request.PositionId,
            request.DepartmentId, request.EffectiveFrom, null));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await departments.GetOverviewAsync(organizationId, request.DepartmentId, cancellationToken)
            ?? throw new NotFoundException("Department not found.");
    }
}
