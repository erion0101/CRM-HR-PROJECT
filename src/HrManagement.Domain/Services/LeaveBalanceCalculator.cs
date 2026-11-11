namespace HrManagement.Domain;

/// <summary>A period during which the person was employed (end inclusive; null = still employed).</summary>
public sealed record EmploymentPeriod(DateOnly Start, DateOnly? End);

/// <summary>Days already booked: pending and approved requests both use up the balance.</summary>
public sealed record BookedLeave(DateOnly Start, DateOnly End, int WorkingDays, LeaveRequestStatus Status);

public sealed record LeaveBalance(
    int Year,
    decimal FullYearAllowance,
    decimal EarnedToDate,
    decimal CarriedOver,
    DateOnly? CarryOverExpiresOn,
    decimal CarryOverExpired,
    decimal Adjustments,
    decimal Approved,
    decimal Pending,
    decimal Available);

/// <summary>
/// Works out a balance from the rules, the employment periods, the booked requests and HR adjustments. Nothing is stored,
/// so cancelling, rejecting or correcting a past request always gives the right result and there is no year-end job to run.
/// <list type="bullet">
/// <item>Monthly accrual earns days per year ÷ 12 for each month in which the person is employed for at least one day.</item>
/// <item>Unused days carry into the next year (up to the limit, if any). Days off use carried-over days first.</item>
/// <item>Carried-over days still unused after the expiry date are lost; only days taken up to that date can use them.</item>
/// </list>
/// </summary>
public static class LeaveBalanceCalculator
{
    public static LeaveBalance Calculate(LeaveType type, IReadOnlyCollection<EmploymentPeriod> employment,
        IReadOnlyCollection<BookedLeave> booked, IReadOnlyDictionary<int, decimal> adjustmentsByYear,
        WorkingDayCalendar calendar, DateOnly asOf)
    {
        var year = asOf.Year;
        var active = booked.Where(x => x.Status is LeaveRequestStatus.Pending or LeaveRequestStatus.Approved).ToArray();
        var carried = CarryInto(type, year, employment, active, adjustmentsByYear, calendar);
        var expiresOn = carried > 0 ? type.CarryOverExpiresOn(year) : null;
        var inYear = active.Where(x => x.Start.Year == year).ToArray();
        var consumed = inYear.Sum(x => x.WorkingDays);
        var carryUsed = CarryUsed(carried, expiresOn, inYear, calendar);
        var expired = expiresOn is { } expiry && asOf > expiry ? carried - carryUsed : 0;
        var adjustments = adjustmentsByYear.GetValueOrDefault(year);
        var earned = Earned(type, year, asOf.Month, employment);
        var available = type.DeductsFromBalance ? earned + adjustments + carried - expired - consumed : 0;

        return new LeaveBalance(year, Earned(type, year, 12, employment), earned, carried, expiresOn, expired, adjustments,
            inYear.Where(x => x.Status == LeaveRequestStatus.Approved).Sum(x => x.WorkingDays),
            inYear.Where(x => x.Status == LeaveRequestStatus.Pending).Sum(x => x.WorkingDays),
            decimal.Round(available, 2));
    }

    /// <summary>Days carried from <paramref name="year"/> − 1 into <paramref name="year"/>, working forward from the first year of employment.</summary>
    private static decimal CarryInto(LeaveType type, int year, IReadOnlyCollection<EmploymentPeriod> employment,
        IReadOnlyCollection<BookedLeave> active, IReadOnlyDictionary<int, decimal> adjustmentsByYear, WorkingDayCalendar calendar)
    {
        if (!type.DeductsFromBalance || !type.CarriesOver || employment.Count == 0) return 0;
        var carry = 0m;
        for (var y = employment.Min(x => x.Start.Year); y < year; y++)
        {
            var inYear = active.Where(x => x.Start.Year == y).ToArray();
            var expiresOn = type.CarryOverExpiresOn(y);
            var carryUsed = CarryUsed(carry, expiresOn, inYear, calendar);
            var carryLeft = expiresOn is null ? carry - carryUsed : 0;
            var ownUsed = inYear.Sum(x => x.WorkingDays) - carryUsed;
            var leftover = Earned(type, y, 12, employment) + adjustmentsByYear.GetValueOrDefault(y) - ownUsed + carryLeft;
            carry = Math.Max(0, leftover);
            if (type.MaxCarryOverDays is { } max) carry = Math.Min(carry, max);
        }
        return decimal.Round(carry, 2);
    }

    /// <summary>Carried-over days used in the year: everything taken (up to the expiry date, when there is one), capped at what was carried.</summary>
    private static decimal CarryUsed(decimal carried, DateOnly? expiresOn, IReadOnlyCollection<BookedLeave> inYear, WorkingDayCalendar calendar)
    {
        if (carried <= 0) return 0;
        var taken = expiresOn is not { } expiry
            ? inYear.Sum(x => x.WorkingDays)
            : inYear.Where(x => x.Start <= expiry)
                .Sum(x => x.End <= expiry ? x.WorkingDays : calendar.CountWorkingDays(x.Start, expiry));
        return Math.Min(carried, taken);
    }

    /// <summary>Allowance earned in <paramref name="year"/> up to and including <paramref name="throughMonth"/>.</summary>
    public static decimal Earned(LeaveType type, int year, int throughMonth, IReadOnlyCollection<EmploymentPeriod> employment)
    {
        if (!type.DeductsFromBalance) return 0;
        var months = Enumerable.Range(1, throughMonth).Count(month => EmployedIn(employment, year, month));
        if (type.AccrualMethod == LeaveAccrualMethod.YearlyUpfront) return months > 0 ? type.DaysPerYear : 0;
        return months == 12 ? type.DaysPerYear : decimal.Round(type.DaysPerYear / 12m * months, 2);
    }

    private static bool EmployedIn(IReadOnlyCollection<EmploymentPeriod> employment, int year, int month)
    {
        var first = new DateOnly(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        return employment.Any(x => x.Start <= last && (x.End is null || x.End >= first));
    }
}
