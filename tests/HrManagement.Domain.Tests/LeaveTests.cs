using HrManagement.Domain;
using Xunit;

namespace HrManagement.Domain.Tests;

public sealed class LeaveTests
{
    private static readonly Guid Org = Guid.NewGuid();
    private static readonly WorkingDayCalendar NoHolidays = new([]);
    private static readonly Dictionary<int, decimal> NoAdjustments = new();

    private static LeaveType Vacation() => LeaveType.Create(Org, "Vacation", "#2563eb", true, true, true, 20,
        LeaveAccrualMethod.Monthly, carriesOver: true, maxCarryOverDays: null, carryOverExpiryMonth: 6, carryOverExpiryDay: 30);

    private static LeaveType Sick() => LeaveType.Create(Org, "Sick leave", "#dc2626", true, true, true, 20,
        LeaveAccrualMethod.YearlyUpfront, carriesOver: false, null, null, null);

    private static EmploymentPeriod[] Since(int year, int month = 1, int day = 1) => [new(new DateOnly(year, month, day), null)];

    private static BookedLeave Approved(DateOnly start, DateOnly end) =>
        new(start, end, NoHolidays.CountWorkingDays(start, end), LeaveRequestStatus.Approved);

    [Fact]
    public void Working_days_skip_weekends_and_holidays()
    {
        var calendar = new WorkingDayCalendar([PublicHoliday.Create(Org, new DateOnly(2026, 1, 1), "New Year", repeatsYearly: true)]);
        // Thu 31 Dec 2026 – Mon 4 Jan 2027: Thu, (Fri 1 Jan holiday), (weekend), Mon.
        Assert.Equal(2, calendar.CountWorkingDays(new DateOnly(2026, 12, 31), new DateOnly(2027, 1, 4)));
        Assert.False(calendar.IsWorkingDay(new DateOnly(2030, 1, 1)));
        Assert.True(calendar.IsWorkingDay(new DateOnly(2025, 1, 1)), "A yearly holiday does not apply before its first year.");
    }

    [Theory]
    [InlineData(1, 1.67)]
    [InlineData(3, 5)]
    [InlineData(6, 10)]
    [InlineData(12, 20)]
    public void Vacation_is_earned_at_about_1_67_days_a_month(int throughMonth, decimal expected) =>
        Assert.Equal(expected, LeaveBalanceCalculator.Earned(Vacation(), 2026, throughMonth, Since(2025)));

    [Fact]
    public void Someone_starting_mid_year_earns_only_for_the_months_they_work()
    {
        var balance = LeaveBalanceCalculator.Calculate(Vacation(), Since(2026, 7, 20), [], NoAdjustments, NoHolidays, new DateOnly(2026, 12, 1));
        Assert.Equal(10, balance.FullYearAllowance); // July–December
        Assert.Equal(10, balance.Available);
    }

    [Fact]
    public void Thirteen_days_taken_leaves_seven_carried_over_and_twenty_seven_next_year()
    {
        var booked = new[] { Approved(new DateOnly(2025, 8, 4), new DateOnly(2025, 8, 20)) }; // 13 working days
        var balance = LeaveBalanceCalculator.Calculate(Vacation(), Since(2025), booked, NoAdjustments, NoHolidays, new DateOnly(2026, 12, 31));
        Assert.Equal(7, balance.CarriedOver);
        Assert.Equal(new DateOnly(2026, 6, 30), balance.CarryOverExpiresOn);
        Assert.Equal(7, balance.CarryOverExpired);
        Assert.Equal(20, balance.Available); // 27 before 30 June; the unused 7 expired after it.

        var inJune = LeaveBalanceCalculator.Calculate(Vacation(), Since(2025), booked, NoAdjustments, NoHolidays, new DateOnly(2026, 6, 15));
        Assert.Equal(17, inJune.Available); // 7 carried + 10 earned Jan–June
    }

    [Fact]
    public void Days_taken_before_30_June_use_carried_over_days_first()
    {
        var booked = new[]
        {
            Approved(new DateOnly(2025, 8, 4), new DateOnly(2025, 8, 20)), // 13 in 2025 → 7 carried
            Approved(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 6))   // 5 in March 2026, from the carried days
        };
        var balance = LeaveBalanceCalculator.Calculate(Vacation(), Since(2025), booked, NoAdjustments, NoHolidays, new DateOnly(2026, 12, 31));
        Assert.Equal(2, balance.CarryOverExpired);
        Assert.Equal(20, balance.Available); // this year's 20 are untouched
    }

    [Fact]
    public void A_request_across_30_June_uses_carried_days_only_up_to_that_date()
    {
        var booked = new[]
        {
            Approved(new DateOnly(2025, 8, 4), new DateOnly(2025, 8, 20)), // 7 carried
            Approved(new DateOnly(2026, 6, 29), new DateOnly(2026, 7, 3))  // Mon–Fri: 2 days by 30 June, 3 after
        };
        var balance = LeaveBalanceCalculator.Calculate(Vacation(), Since(2025), booked, NoAdjustments, NoHolidays, new DateOnly(2026, 12, 31));
        Assert.Equal(5, balance.CarryOverExpired);
        Assert.Equal(17, balance.Available);
    }

    [Fact]
    public void Sick_leave_has_its_own_twenty_days_and_does_not_carry_over()
    {
        var booked = new[] { Approved(new DateOnly(2025, 3, 2), new DateOnly(2025, 3, 4)) };
        var balance = LeaveBalanceCalculator.Calculate(Sick(), Since(2025), booked, NoAdjustments, NoHolidays, new DateOnly(2026, 2, 1));
        Assert.Equal(0, balance.CarriedOver);
        Assert.Equal(20, balance.Available);
    }

    [Fact]
    public void Pending_requests_reserve_days_and_rejected_ones_do_not()
    {
        var booked = new BookedLeave[]
        {
            new(new DateOnly(2026, 5, 4), new DateOnly(2026, 5, 5), 2, LeaveRequestStatus.Pending),
            new(new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 13), 3, LeaveRequestStatus.Rejected)
        };
        var balance = LeaveBalanceCalculator.Calculate(Vacation(), Since(2026), booked, NoAdjustments, NoHolidays, new DateOnly(2026, 12, 1));
        Assert.Equal(2, balance.Pending);
        Assert.Equal(0, balance.Approved);
        Assert.Equal(18, balance.Available);
    }

    [Fact]
    public void Adjustments_change_the_balance_and_what_carries_over()
    {
        var adjustments = new Dictionary<int, decimal> { [2025] = 3 };
        var balance = LeaveBalanceCalculator.Calculate(Vacation(), Since(2025), [], adjustments, NoHolidays, new DateOnly(2026, 1, 15));
        Assert.Equal(23, balance.CarriedOver);
    }

    [Fact]
    public void A_carry_over_limit_caps_the_days_carried()
    {
        var type = LeaveType.Create(Org, "Vacation", "#2563eb", true, true, true, 20, LeaveAccrualMethod.Monthly, true, 5, null, null);
        var balance = LeaveBalanceCalculator.Calculate(type, Since(2025), [], NoAdjustments, NoHolidays, new DateOnly(2026, 1, 15));
        Assert.Equal(5, balance.CarriedOver);
    }

    [Fact]
    public void Requests_cannot_cross_into_a_new_year_or_be_empty()
    {
        Assert.Throws<DomainException>(() => LeaveRequest.Create(Org, Guid.NewGuid(), Vacation(), new DateOnly(2026, 12, 30),
            new DateOnly(2027, 1, 2), 3, null, null, DateTimeOffset.UtcNow));
        Assert.Throws<DomainException>(() => LeaveRequest.Create(Org, Guid.NewGuid(), Vacation(), new DateOnly(2026, 11, 14),
            new DateOnly(2026, 11, 15), 0, null, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Approved_time_off_can_be_cancelled_only_before_it_starts()
    {
        var request = LeaveRequest.Create(Org, Guid.NewGuid(), Vacation(), new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 4), 4, null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        request.Approve(Guid.NewGuid(), null, DateTimeOffset.UtcNow);
        Assert.Throws<DomainException>(() => request.Cancel(new DateOnly(2026, 12, 2), DateTimeOffset.UtcNow));
        request.Cancel(new DateOnly(2026, 11, 20), DateTimeOffset.UtcNow);
        Assert.Equal(LeaveRequestStatus.Cancelled, request.Status);
    }

    [Fact]
    public void Rejecting_needs_a_reason_and_a_decision_is_final()
    {
        var request = LeaveRequest.Create(Org, Guid.NewGuid(), Vacation(), new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 4), 4, null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<DomainException>(() => request.Reject(Guid.NewGuid(), " ", DateTimeOffset.UtcNow));
        request.Reject(Guid.NewGuid(), "Release week", DateTimeOffset.UtcNow);
        Assert.Throws<DomainException>(() => request.Approve(Guid.NewGuid(), null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Types_that_need_no_approval_are_approved_straight_away()
    {
        var type = LeaveType.Create(Org, "Sick leave", "#dc2626", true, requiresApproval: false, true, 20, LeaveAccrualMethod.YearlyUpfront, false, null, null, null);
        var request = LeaveRequest.Create(Org, Guid.NewGuid(), type, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 1), 1, null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(LeaveRequestStatus.Approved, request.Status);
    }
}
