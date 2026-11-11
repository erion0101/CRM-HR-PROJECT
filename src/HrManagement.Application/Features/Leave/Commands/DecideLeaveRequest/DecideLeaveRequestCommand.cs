using FluentValidation;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Leave.Commands.DecideLeaveRequest;

public sealed record DecideLeaveRequestCommand(Guid RequestId, bool Approve, string? Comment) : ICommand<LeaveRequestDto>;

public sealed class DecideLeaveRequestCommandValidator : AbstractValidator<DecideLeaveRequestCommand>
{
    public DecideLeaveRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.Comment).MaximumLength(500);
        RuleFor(x => x.Comment).NotEmpty().When(x => !x.Approve).WithMessage("Say why the request is rejected.");
    }
}

/// <summary>The employee's current manager, or HR, approves or rejects a pending request.</summary>
public sealed class DecideLeaveRequestCommandHandler(ILeaveRepository repository, LeaveService leave, LeaveNotifier notifier, IUnitOfWork unitOfWork)
    : ICommandHandler<DecideLeaveRequestCommand, LeaveRequestDto>
{
    public async Task<LeaveRequestDto> Handle(DecideLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var item = await repository.GetRequestAsync(request.RequestId, cancellationToken) ?? throw new NotFoundException("Request not found.");
        if (!await leave.CanDecideAsync(item, cancellationToken))
            throw new ForbiddenException("Only the employee's manager or HR can decide this request.");
        var type = await repository.GetTypeAsync(item.LeaveTypeId, cancellationToken) ?? throw new NotFoundException("Leave type not found.");
        var userId = leave.RequireUserId();
        if (request.Approve)
        {
            item.Approve(userId, request.Comment, DateTimeOffset.UtcNow);
            notifier.Approved(item, type);
        }
        else
        {
            item.Reject(userId, request.Comment ?? string.Empty, DateTimeOffset.UtcNow);
            notifier.Rejected(item, type);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await leave.ToDtosAsync([item], cancellationToken))[0];
    }
}
