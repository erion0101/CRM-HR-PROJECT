using HrManagement.Domain;

namespace HrManagement.Application.Features.Leave;

public sealed record LeaveTypeDto(Guid Id, string Name, string Color, bool IsPaid, bool RequiresApproval, bool DeductsFromBalance,
    decimal DaysPerYear, LeaveAccrualMethod AccrualMethod, bool CarriesOver, decimal? MaxCarryOverDays,
    int? CarryOverExpiryMonth, int? CarryOverExpiryDay, bool IsArchived, long Version);

public sealed record PublicHolidayDto(Guid Id, DateOnly Date, string Name, bool RepeatsYearly);

public sealed record LeaveBalanceDto(Guid LeaveTypeId, string LeaveTypeName, string Color, bool DeductsFromBalance, int Year,
    decimal FullYearAllowance, decimal EarnedToDate, decimal CarriedOver, DateOnly? CarryOverExpiresOn, decimal CarryOverExpired,
    decimal Adjustments, decimal Approved, decimal Pending, decimal Available);

public sealed record LeaveRequestDto(Guid Id, Guid EmployeeId, string EmployeeName, Guid LeaveTypeId, string LeaveTypeName, string Color,
    DateOnly StartDate, DateOnly EndDate, int WorkingDays, string? Reason, LeaveRequestStatus Status, string? ApproverName,
    string? DecisionComment, DateTimeOffset? DecidedAtUtc, DateTimeOffset CreatedAtUtc, bool CanCancel);

public sealed record LeaveAdjustmentDto(Guid Id, Guid LeaveTypeId, string LeaveTypeName, int Year, decimal Days, string Reason, DateTimeOffset CreatedAtUtc);

public sealed record EmployeeLeaveDto(Guid EmployeeId, string EmployeeName, int Year, IReadOnlyList<LeaveBalanceDto> Balances,
    IReadOnlyList<LeaveRequestDto> Requests, IReadOnlyList<LeaveAdjustmentDto> Adjustments);

/// <summary>What a request would cost, shown live while the person picks dates. <see cref="Problem"/> explains why it cannot be sent.</summary>
public sealed record LeavePreviewDto(int WorkingDays, decimal? AvailableBefore, decimal? AvailableAfter, IReadOnlyList<PublicHolidayDto> Holidays, string? Problem);

public sealed record LeaveApprovalDto(LeaveRequestDto Request, decimal? AvailableAfter, IReadOnlyList<string> TeammatesOut);

public sealed record LeaveCalendarEntryDto(Guid EmployeeId, string EmployeeName, DateOnly StartDate, DateOnly EndDate,
    LeaveRequestStatus Status, string? LeaveTypeName, string? Color);

public sealed record LeaveCalendarDto(DateOnly From, DateOnly To, IReadOnlyList<PublicHolidayDto> Holidays, IReadOnlyList<LeaveCalendarEntryDto> Entries);
