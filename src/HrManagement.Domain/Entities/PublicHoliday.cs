namespace HrManagement.Domain;

/// <summary>A day nobody works. A yearly holiday repeats on the same month and day from its first year onward.</summary>
public sealed class PublicHoliday : Entity
{
    private PublicHoliday() { }
    public DateOnly Date { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool RepeatsYearly { get; private set; }

    public static PublicHoliday Create(Guid organizationId, DateOnly date, string name, bool repeatsYearly) => new()
    {
        OrganizationId = organizationId,
        Date = date,
        Name = Guard.Required(name, nameof(name), 120),
        RepeatsYearly = repeatsYearly
    };

    /// <summary>The date this holiday falls on in <paramref name="year"/>, or null when it does not occur that year.</summary>
    public DateOnly? OccursIn(int year)
    {
        if (!RepeatsYearly) return Date.Year == year ? Date : null;
        if (year < Date.Year) return null;
        // 29 February repeats only in leap years.
        return Date.Month == 2 && Date.Day == 29 && !DateTime.IsLeapYear(year) ? null : new DateOnly(year, Date.Month, Date.Day);
    }
}
