namespace HrManagement.Domain;

/// <summary>Counts working days: Monday to Friday, minus public holidays.</summary>
public sealed class WorkingDayCalendar
{
    private readonly IReadOnlyCollection<PublicHoliday> _holidays;
    private readonly Dictionary<int, HashSet<DateOnly>> _byYear = new();

    public WorkingDayCalendar(IEnumerable<PublicHoliday> holidays) => _holidays = holidays.ToArray();

    public bool IsWorkingDay(DateOnly day) =>
        day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !HolidaysIn(day.Year).Contains(day);

    public int CountWorkingDays(DateOnly start, DateOnly end)
    {
        var count = 0;
        for (var day = start; day <= end; day = day.AddDays(1))
            if (IsWorkingDay(day)) count++;
        return count;
    }

    public IReadOnlySet<DateOnly> HolidaysIn(int year)
    {
        if (_byYear.TryGetValue(year, out var days)) return days;
        days = _holidays.Select(x => x.OccursIn(year)).OfType<DateOnly>().ToHashSet();
        _byYear[year] = days;
        return days;
    }
}
