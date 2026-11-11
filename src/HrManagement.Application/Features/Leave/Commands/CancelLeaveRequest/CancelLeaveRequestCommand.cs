using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Leave.Commands.CancelLeaveRequest;

public sealed record CancelLeaveRequestCommand(Guid RequestId) : ICommand<LeaveRequestDto>;

/// <summary>The employee cancels their own request; HR can cancel anyone's. Cancelling gives the days back.</summary>
public sealed class CancelLeaveRequestCommandHandler(ILeaveRepository repository, LeaveService leave, LeaveNotifier notifier,
    ICurrentUserService currentUser, IUnitOfWork unitOfWork) : ICommandHandler<CancelLeaveRequestCommand, LeaveRequestDto>
{
    public async Task<LeaveRequestDto> Handle(CancelLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var item = await repository.GetRequestAsync(request.RequestId, cancellationToken) ?? throw new NotFoundException("Request not found.");
        if (item.EmployeeId != currentUser.EmployeeId && !leave.IsHr) throw new NotFoundException("Request not found.");
        item.Cancel(LeaveService.Today, DateTimeOffset.UtcNow);
        var type = await repository.GetTypeAsync(item.LeaveTypeId, cancellationToken) ?? throw new NotFoundException("Leave type not found.");
        await notifier.CancelledAsync(item, type, currentUser.EmployeeId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await leave.ToDtosAsync([item], cancellationToken))[0];
    }
}
