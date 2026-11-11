using HrManagement.Domain;

namespace HrManagement.Application.Features.Leave;

internal static class LeaveMappings
{
    public static LeaveTypeDto ToDto(this LeaveType x) => new(x.Id, x.Name, x.Color, x.IsPaid, x.RequiresApproval, x.DeductsFromBalance,
        x.DaysPerYear, x.AccrualMethod, x.CarriesOver, x.MaxCarryOverDays, x.CarryOverExpiryMonth, x.CarryOverExpiryDay, x.IsDeleted, x.Version);

    public static PublicHolidayDto ToDto(this PublicHoliday x) => new(x.Id, x.Date, x.Name, x.RepeatsYearly);

    public static LeaveBalanceDto ToDto(this LeaveBalance x, LeaveType type) => new(type.Id, type.Name, type.Color, type.DeductsFromBalance,
        x.Year, x.FullYearAllowance, x.EarnedToDate, x.CarriedOver, x.CarryOverExpiresOn, x.CarryOverExpired, x.Adjustments,
        x.Approved, x.Pending, x.Available);
}
