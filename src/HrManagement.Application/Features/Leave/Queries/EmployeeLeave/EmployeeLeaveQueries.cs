using FluentValidation;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Leave.Queries.EmployeeLeave;

public sealed record GetMyLeaveQuery(int? Year) : IQuery<EmployeeLeaveDto>;
public sealed record GetEmployeeLeaveQuery(Guid EmployeeId, int? Year) : IQuery<EmployeeLeaveDto>;
public sealed record PreviewLeaveRequestQuery(Guid LeaveTypeId, DateOnly StartDate, DateOnly EndDate) : IQuery<LeavePreviewDto>;

public sealed class GetMyLeaveQueryHandler(LeaveService leave) : IQueryHandler<GetMyLeaveQuery, EmployeeLeaveDto>
{
    public Task<EmployeeLeaveDto> Handle(GetMyLeaveQuery request, CancellationToken cancellationToken) =>
        leave.EmployeeLeaveAsync(leave.RequireEmployeeId(), request.Year, cancellationToken);
}

/// <summary>For the employee profile: HR, the person's manager, or the person themselves.</summary>
public sealed class GetEmployeeLeaveQueryHandler(LeaveService leave, IAuthorizedDataScope scope) : IQueryHandler<GetEmployeeLeaveQuery, EmployeeLeaveDto>
{
    public async Task<EmployeeLeaveDto> Handle(GetEmployeeLeaveQuery request, CancellationToken cancellationToken)
    {
        if (!await scope.CanAccessEmployeeAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        return await leave.EmployeeLeaveAsync(request.EmployeeId, request.Year, cancellationToken);
    }
}

public sealed class PreviewLeaveRequestQueryValidator : AbstractValidator<PreviewLeaveRequestQuery>
{
    public PreviewLeaveRequestQueryValidator() => RuleFor(x => x.LeaveTypeId).NotEmpty();
}

public sealed class PreviewLeaveRequestQueryHandler(ILeaveRepository repository, LeaveService leave) : IQueryHandler<PreviewLeaveRequestQuery, LeavePreviewDto>
{
    public async Task<LeavePreviewDto> Handle(PreviewLeaveRequestQuery request, CancellationToken cancellationToken)
    {
        var employeeId = leave.RequireEmployeeId();
        var type = await repository.GetTypeAsync(request.LeaveTypeId, cancellationToken) ?? throw new NotFoundException("Leave type not found.");
        var context = await leave.LoadAsync(employeeId, cancellationToken);
        return LeaveService.Evaluate(context, type, request.StartDate, request.EndDate, await repository.ListHolidaysAsync(cancellationToken));
    }
}
