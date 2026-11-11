using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Leave.Commands.SaveLeaveType;

public sealed record LeaveTypeInput(string Name, string Color, bool IsPaid, bool RequiresApproval, bool DeductsFromBalance,
    decimal DaysPerYear, LeaveAccrualMethod AccrualMethod, bool CarriesOver, decimal? MaxCarryOverDays,
    int? CarryOverExpiryMonth, int? CarryOverExpiryDay);

public sealed record CreateLeaveTypeCommand(LeaveTypeInput Input) : ICommand<LeaveTypeDto>;
public sealed record UpdateLeaveTypeCommand(Guid Id, LeaveTypeInput Input, long ExpectedVersion) : ICommand<LeaveTypeDto>;
public sealed record ArchiveLeaveTypeCommand(Guid Id, long ExpectedVersion) : ICommand;
