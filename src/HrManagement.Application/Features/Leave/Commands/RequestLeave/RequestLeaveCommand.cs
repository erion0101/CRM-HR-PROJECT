using FluentValidation;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Leave.Commands.RequestLeave;

public sealed record RequestLeaveCommand(Guid LeaveTypeId, DateOnly StartDate, DateOnly EndDate, string? Reason) : ICommand<LeaveRequestDto>;

public sealed class RequestLeaveCommandValidator : AbstractValidator<RequestLeaveCommand>
{
    public RequestLeaveCommandValidator()
    {
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).WithMessage("The end date cannot be before the start date.");
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

/// <summary>The signed-in employee asks for time off for themselves. Their current manager approves it.</summary>
public sealed class RequestLeaveCommandHandler(ILeaveRepository repository, LeaveService leave, LeaveNotifier notifier, IUnitOfWork unitOfWork)
    : ICommandHandler<RequestLeaveCommand, LeaveRequestDto>
{
    public async Task<LeaveRequestDto> Handle(RequestLeaveCommand request, CancellationToken cancellationToken)
    {
        var employeeId = leave.RequireEmployeeId();
        var type = await repository.GetTypeAsync(request.LeaveTypeId, cancellationToken) ?? throw new NotFoundException("Leave type not found.");
        var context = await leave.LoadAsync(employeeId, cancellationToken);
        var check = LeaveService.Evaluate(context, type, request.StartDate, request.EndDate, await repository.ListHolidaysAsync(cancellationToken));
        if (check.Problem is { } problem) throw new DomainException(problem);

        var manager = await repository.GetCurrentManagerAsync(employeeId, LeaveService.Today, cancellationToken);
        var created = LeaveRequest.Create(type.OrganizationId, employeeId, type, request.StartDate, request.EndDate, check.WorkingDays,
            request.Reason, manager, DateTimeOffset.UtcNow);
        repository.AddRequest(created);
        await notifier.RequestedAsync(created, type, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await leave.ToDtosAsync([created], cancellationToken))[0];
    }
}
