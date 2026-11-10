using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Employees;

namespace HrManagement.Application.Features.Departments.Commands.RemoveDepartmentEmployee;

public sealed class RemoveDepartmentEmployeeCommandHandler(
    IDepartmentRepository departments,
    IEmployeeRepository employees,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : ICommandHandler<RemoveDepartmentEmployeeCommand, DepartmentOverviewDto>
{
    public async Task<DepartmentOverviewDto> Handle(RemoveDepartmentEmployeeCommand request, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        _ = await departments.GetAsync(organizationId, request.DepartmentId, cancellationToken)
            ?? throw new NotFoundException("Department not found.");
        _ = await employees.GetAccessibleAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee not found.");
        var assignment = await employees.GetAssignmentOnDateAsync(request.EmployeeId, request.EffectiveOn, cancellationToken);
        if (assignment is null || assignment.DepartmentId != request.DepartmentId)
            throw new NotFoundException("Current department assignment not found.");

        var finalActiveDate = request.EffectiveOn.AddDays(-1);
        if (finalActiveDate < assignment.EffectiveFrom)
            throw new ConflictException("An assignment that starts today cannot be ended before it becomes active.");
        assignment.End(finalActiveDate, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await departments.GetOverviewAsync(organizationId, request.DepartmentId, cancellationToken)
            ?? throw new NotFoundException("Department not found.");
    }
}
