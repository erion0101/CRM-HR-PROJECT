using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Leave.Commands.SaveLeaveType;

public sealed class CreateLeaveTypeCommandHandler(ILeaveRepository repository, IAuthorizedDataScope scope, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateLeaveTypeCommand, LeaveTypeDto>
{
    public async Task<LeaveTypeDto> Handle(CreateLeaveTypeCommand request, CancellationToken cancellationToken)
    {
        var x = request.Input;
        if (await repository.TypeNameExistsAsync(x.Name.Trim(), null, cancellationToken)) throw new ConflictException("A leave type with this name already exists.");
        var type = LeaveType.Create(scope.OrganizationId, x.Name, x.Color, x.IsPaid, x.RequiresApproval, x.DeductsFromBalance, x.DaysPerYear,
            x.AccrualMethod, x.CarriesOver, x.MaxCarryOverDays, x.CarryOverExpiryMonth, x.CarryOverExpiryDay);
        repository.AddType(type);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return type.ToDto();
    }
}

public sealed class UpdateLeaveTypeCommandHandler(ILeaveRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateLeaveTypeCommand, LeaveTypeDto>
{
    public async Task<LeaveTypeDto> Handle(UpdateLeaveTypeCommand request, CancellationToken cancellationToken)
    {
        var type = await repository.GetTypeAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Leave type not found.");
        if (type.Version != request.ExpectedVersion) throw new ConflictException("The record was changed by another user.");
        var x = request.Input;
        if (await repository.TypeNameExistsAsync(x.Name.Trim(), type.Id, cancellationToken)) throw new ConflictException("A leave type with this name already exists.");
        type.Update(x.Name, x.Color, x.IsPaid, x.RequiresApproval, x.DeductsFromBalance, x.DaysPerYear, x.AccrualMethod, x.CarriesOver,
            x.MaxCarryOverDays, x.CarryOverExpiryMonth, x.CarryOverExpiryDay, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return type.ToDto();
    }
}

/// <summary>Archived types cannot be requested any more; existing requests and balances keep them.</summary>
public sealed class ArchiveLeaveTypeCommandHandler(ILeaveRepository repository, IUnitOfWork unitOfWork) : ICommandHandler<ArchiveLeaveTypeCommand>
{
    public async Task Handle(ArchiveLeaveTypeCommand request, CancellationToken cancellationToken)
    {
        var type = await repository.GetTypeAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Leave type not found.");
        if (type.Version != request.ExpectedVersion) throw new ConflictException("The record was changed by another user.");
        type.Archive(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
