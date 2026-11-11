using FluentValidation;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Leave.Commands.AdjustLeaveBalance;

public sealed record AdjustLeaveBalanceCommand(Guid EmployeeId, Guid LeaveTypeId, int Year, decimal Days, string Reason) : ICommand<EmployeeLeaveDto>;

public sealed class AdjustLeaveBalanceCommandValidator : AbstractValidator<AdjustLeaveBalanceCommand>
{
    public AdjustLeaveBalanceCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Days).NotEqual(0).InclusiveBetween(-366, 366);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

/// <summary>HR adds or removes days, for example days agreed in a contract. Every change is kept with its reason.</summary>
public sealed class AdjustLeaveBalanceCommandHandler(ILeaveRepository repository, LeaveService leave, IAuthorizedDataScope scope,
    IUnitOfWork unitOfWork) : ICommandHandler<AdjustLeaveBalanceCommand, EmployeeLeaveDto>
{
    public async Task<EmployeeLeaveDto> Handle(AdjustLeaveBalanceCommand request, CancellationToken cancellationToken)
    {
        if (!await scope.CanAccessEmployeeAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        var type = await repository.GetTypeAsync(request.LeaveTypeId, cancellationToken) ?? throw new NotFoundException("Leave type not found.");
        if (!type.DeductsFromBalance) throw new DomainException($"{type.Name} has no balance to adjust.");
        repository.AddAdjustment(LeaveBalanceAdjustment.Create(scope.OrganizationId, request.EmployeeId, type.Id, request.Year,
            request.Days, request.Reason, leave.RequireUserId()));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await leave.EmployeeLeaveAsync(request.EmployeeId, request.Year, cancellationToken);
    }
}
