using HrManagement.Application.Common.Authorization;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Leave;

/// <summary>Everything needed to work out one person's balances.</summary>
public sealed record LeaveContext(Guid EmployeeId, IReadOnlyList<LeaveType> Types, WorkingDayCalendar Calendar,
    IReadOnlyList<EmploymentPeriod> Employment, IReadOnlyList<LeaveRequest> Requests, IReadOnlyList<LeaveBalanceAdjustment> Adjustments)
{
    public LeaveBalance BalanceFor(LeaveType type, DateOnly asOf, LeaveRequest? extra = null)
    {
        var booked = Requests.Append(extra).OfType<LeaveRequest>().Where(x => x.LeaveTypeId == type.Id)
            .Select(x => new BookedLeave(x.StartDate, x.EndDate, x.WorkingDays, x.Status)).ToArray();
        var adjustments = Adjustments.Where(x => x.LeaveTypeId == type.Id).GroupBy(x => x.Year).ToDictionary(x => x.Key, x => x.Sum(a => a.Days));
        return LeaveBalanceCalculator.Calculate(type, Employment, booked, adjustments, Calendar, asOf);
    }
}

/// <summary>Shared rules for leave handlers: who is asking, balances, the request check and the DTOs.</summary>
public sealed class LeaveService(ILeaveRepository repository, ICurrentUserService currentUser)
{
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
    public bool IsHr => currentUser.IsInRole(Roles.Hr);

    public Guid RequireUserId() => currentUser.UserId ?? throw new ForbiddenException();

    public Guid RequireEmployeeId() => currentUser.EmployeeId ??
        throw new ForbiddenException("Your account is not linked to an employee profile, so it has no time off.");

    /// <summary>HR may decide anyone's request; a manager only their current direct reports'. Nobody decides their own.</summary>
    public async Task<bool> CanDecideAsync(LeaveRequest request, CancellationToken cancellationToken)
    {
        if (request.EmployeeId == currentUser.EmployeeId) return false;
        if (IsHr) return true;
        return currentUser.EmployeeId is { } self &&
            await repository.GetCurrentManagerAsync(request.EmployeeId, Today, cancellationToken) == self;
    }

    public async Task<LeaveContext> LoadAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var types = await repository.ListTypesAsync(includeArchived: true, cancellationToken);
        var holidays = await repository.ListHolidaysAsync(cancellationToken);
        return new LeaveContext(employeeId, types, new WorkingDayCalendar(holidays),
            await repository.GetEmploymentPeriodsAsync(employeeId, cancellationToken),
            await repository.ListRequestsForEmployeeAsync(employeeId, cancellationToken),
            await repository.ListAdjustmentsAsync(employeeId, cancellationToken));
    }

    /// <summary>Checks a new request against the rules and returns what it would cost.</summary>
    public static LeavePreviewDto Evaluate(LeaveContext context, LeaveType type, DateOnly start, DateOnly end, IReadOnlyList<PublicHoliday> holidays)
    {
        var inRange = holidays.Select(h => (Holiday: h, On: start.Year == end.Year ? h.OccursIn(start.Year) : null))
            .Where(x => x.On is { } on && on >= start && on <= end)
            .Select(x => new PublicHolidayDto(x.Holiday.Id, x.On!.Value, x.Holiday.Name, x.Holiday.RepeatsYearly))
            .OrderBy(x => x.Date).ToArray();
        LeavePreviewDto Fail(int days, string problem) => new(days, null, null, inRange, problem);

        if (end < start) return Fail(0, "The end date cannot be before the start date.");
        if (start.Year != end.Year) return Fail(0, "Time off cannot cross into a new year. Make one request for each year.");
        if (end.DayNumber - start.DayNumber > 92) return Fail(0, "A single request cannot be longer than three months.");
        if (type.IsDeleted) return Fail(0, "This leave type is no longer available.");
        var workingDays = context.Calendar.CountWorkingDays(start, end);
        if (workingDays == 0) return Fail(0, "These dates are only weekends or public holidays.");
        if (!context.Employment.Any(x => x.Start <= start && (x.End is null || x.End >= end)))
            return Fail(workingDays, "You are not employed for the whole of this period.");
        if (context.Requests.Any(x => x.IsActive && x.StartDate <= end && x.EndDate >= start))
            return Fail(workingDays, "You already have time off booked on some of these days.");
        if (!type.DeductsFromBalance) return new LeavePreviewDto(workingDays, null, null, inRange, null);

        var asOf = start > Today ? start : Today.Year == start.Year ? Today : new DateOnly(start.Year, 12, 31);
        var before = context.BalanceFor(type, asOf).Available;
        var after = before - workingDays;
        var problem = after < 0
            ? $"Not enough {type.Name.ToLowerInvariant()} days: you will have {before:0.##} by {start:d MMM yyyy} and this needs {workingDays}."
            : null;
        return new LeavePreviewDto(workingDays, before, after, inRange, problem);
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> ToDtosAsync(IReadOnlyCollection<LeaveRequest> requests, CancellationToken cancellationToken)
    {
        var types = (await repository.ListTypesAsync(includeArchived: true, cancellationToken)).ToDictionary(x => x.Id);
        var names = await repository.GetEmployeeNamesAsync(requests.Select(x => x.EmployeeId)
            .Concat(requests.Select(x => x.ApproverEmployeeId).OfType<Guid>()), cancellationToken);
        var today = Today;
        return requests.Select(x =>
        {
            var type = types.GetValueOrDefault(x.LeaveTypeId);
            var canCancel = (x.EmployeeId == currentUser.EmployeeId || IsHr) &&
                (x.Status == LeaveRequestStatus.Pending || x.Status == LeaveRequestStatus.Approved && x.StartDate > today);
            return new LeaveRequestDto(x.Id, x.EmployeeId, names.GetValueOrDefault(x.EmployeeId, "Unknown employee"), x.LeaveTypeId,
                type?.Name ?? "Leave", type?.Color ?? "#64748b", x.StartDate, x.EndDate, x.WorkingDays, x.Reason, x.Status,
                x.ApproverEmployeeId is { } approver ? names.GetValueOrDefault(approver) : null,
                x.DecisionComment, x.DecidedAtUtc, x.CreatedAtUtc, canCancel);
        }).ToArray();
    }

    public async Task<EmployeeLeaveDto> EmployeeLeaveAsync(Guid employeeId, int? year, CancellationToken cancellationToken)
    {
        var context = await LoadAsync(employeeId, cancellationToken);
        var today = Today;
        var selectedYear = year ?? today.Year;
        var asOf = selectedYear == today.Year ? today : new DateOnly(selectedYear, 12, 31);
        var activeTypes = context.Types.Where(x => !x.IsDeleted || context.Requests.Any(r => r.LeaveTypeId == x.Id && r.StartDate.Year == selectedYear));
        var balances = activeTypes.OrderByDescending(x => x.DeductsFromBalance).ThenBy(x => x.CreatedAtUtc).ThenBy(x => x.Name)
            .Select(type => context.BalanceFor(type, asOf).ToDto(type)).ToArray();
        var names = await repository.GetEmployeeNamesAsync([employeeId], cancellationToken);
        var typeNames = context.Types.ToDictionary(x => x.Id, x => x.Name);
        var adjustments = context.Adjustments.Where(x => x.Year == selectedYear).OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new LeaveAdjustmentDto(x.Id, x.LeaveTypeId, typeNames.GetValueOrDefault(x.LeaveTypeId, "Leave"), x.Year, x.Days, x.Reason, x.CreatedAtUtc))
            .ToArray();
        var requests = context.Requests.Where(x => x.StartDate.Year == selectedYear || x.StartDate >= today)
            .OrderByDescending(x => x.StartDate).ToArray();
        return new EmployeeLeaveDto(employeeId, names.GetValueOrDefault(employeeId, "Unknown employee"), selectedYear, balances,
            await ToDtosAsync(requests, cancellationToken), adjustments);
    }
}
