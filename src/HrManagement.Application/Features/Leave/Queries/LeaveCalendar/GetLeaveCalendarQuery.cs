using FluentValidation;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Leave.Queries.LeaveCalendar;

public sealed record GetLeaveCalendarQuery(DateOnly From, DateOnly To, Guid? DepartmentId) : IQuery<LeaveCalendarDto>;

public sealed class GetLeaveCalendarQueryValidator : AbstractValidator<GetLeaveCalendarQuery>
{
    public GetLeaveCalendarQueryValidator()
    {
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From);
        RuleFor(x => x).Must(x => x.To.DayNumber - x.From.DayNumber <= 62).WithMessage("The calendar can show at most two months at a time.");
    }
}

/// <summary>
/// Who is out, and public holidays. HR sees everyone (optionally one department). Others see their own team:
/// themselves, their manager, colleagues with the same manager, and their own direct reports.
/// The kind of leave is shown only to the person, their manager and HR; everyone else just sees "Out".
/// </summary>
public sealed class GetLeaveCalendarQueryHandler(ILeaveRepository repository, LeaveService leave, IAuthorizedDataScope scope,
    ICurrentUserService currentUser) : IQueryHandler<GetLeaveCalendarQuery, LeaveCalendarDto>
{
    public async Task<LeaveCalendarDto> Handle(GetLeaveCalendarQuery request, CancellationToken cancellationToken)
    {
        var today = LeaveService.Today;
        var self = currentUser.EmployeeId;
        var reports = self is { } caller ? await repository.GetDirectReportsAsync(caller, today, cancellationToken) : [];
        IReadOnlyCollection<Guid> people;
        if (leave.IsHr)
        {
            people = request.DepartmentId is { } department
                ? await repository.GetDepartmentEmployeesAsync(department, today, cancellationToken)
                : await scope.GetAccessibleEmployeeIdsAsync(cancellationToken);
        }
        else if (self is { } me)
        {
            var team = new HashSet<Guid> { me };
            team.UnionWith(reports);
            if (await repository.GetCurrentManagerAsync(me, today, cancellationToken) is { } manager)
            {
                team.Add(manager);
                team.UnionWith(await repository.GetDirectReportsAsync(manager, today, cancellationToken));
            }
            people = team;
        }
        else people = [];

        var entries = people.Count == 0 ? [] : await repository.ListActiveOverlappingAsync(people, request.From, request.To, cancellationToken);
        var types = (await repository.ListTypesAsync(includeArchived: true, cancellationToken)).ToDictionary(x => x.Id);
        var names = await repository.GetEmployeeNamesAsync(entries.Select(x => x.EmployeeId), cancellationToken);
        var reportSet = reports.ToHashSet();
        var visible = entries
            .Where(x => x.Status == LeaveRequestStatus.Approved || x.EmployeeId == self || leave.IsHr || reportSet.Contains(x.EmployeeId))
            .OrderBy(x => x.StartDate).Select(x =>
            {
                var detailed = leave.IsHr || x.EmployeeId == self || reportSet.Contains(x.EmployeeId);
                var type = types.GetValueOrDefault(x.LeaveTypeId);
                return new LeaveCalendarEntryDto(x.EmployeeId, names.GetValueOrDefault(x.EmployeeId, "Colleague"), x.StartDate, x.EndDate,
                    x.Status, detailed ? type?.Name : null, detailed ? type?.Color : null);
            }).ToArray();

        var holidays = (await repository.ListHolidaysAsync(cancellationToken))
            .SelectMany(h => Enumerable.Range(request.From.Year, request.To.Year - request.From.Year + 1)
                .Select(year => h.OccursIn(year)).OfType<DateOnly>()
                .Where(on => on >= request.From && on <= request.To)
                .Select(on => new PublicHolidayDto(h.Id, on, h.Name, h.RepeatsYearly)))
            .OrderBy(x => x.Date).ToArray();
        return new LeaveCalendarDto(request.From, request.To, holidays, visible);
    }
}
