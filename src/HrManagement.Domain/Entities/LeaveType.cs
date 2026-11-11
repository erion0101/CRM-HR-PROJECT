namespace HrManagement.Domain;

/// <summary>A kind of time off (Vacation, Sick leave) with the organization's allowance rules for it.</summary>
public sealed class LeaveType : SoftDeletableEntity
{
    private LeaveType() { }
    public string Name { get; private set; } = string.Empty;
    public string Color { get; private set; } = "#2563eb";
    public bool IsPaid { get; private set; }
    public bool RequiresApproval { get; private set; }
    /// <summary>When false the type has no allowance (for example unpaid leave); days are only counted.</summary>
    public bool DeductsFromBalance { get; private set; }
    public decimal DaysPerYear { get; private set; }
    public LeaveAccrualMethod AccrualMethod { get; private set; }
    public bool CarriesOver { get; private set; }
    /// <summary>Upper limit on days carried into the next year; null means no limit.</summary>
    public decimal? MaxCarryOverDays { get; private set; }
    /// <summary>Carried-over days still unused after this day of the following year expire; null means they never expire.</summary>
    public int? CarryOverExpiryMonth { get; private set; }
    public int? CarryOverExpiryDay { get; private set; }

    public static LeaveType Create(Guid organizationId, string name, string color, bool isPaid, bool requiresApproval,
        bool deductsFromBalance, decimal daysPerYear, LeaveAccrualMethod accrualMethod, bool carriesOver,
        decimal? maxCarryOverDays, int? carryOverExpiryMonth, int? carryOverExpiryDay)
    {
        var type = new LeaveType { OrganizationId = organizationId };
        type.Apply(name, color, isPaid, requiresApproval, deductsFromBalance, daysPerYear, accrualMethod, carriesOver,
            maxCarryOverDays, carryOverExpiryMonth, carryOverExpiryDay);
        return type;
    }

    public void Update(string name, string color, bool isPaid, bool requiresApproval, bool deductsFromBalance,
        decimal daysPerYear, LeaveAccrualMethod accrualMethod, bool carriesOver, decimal? maxCarryOverDays,
        int? carryOverExpiryMonth, int? carryOverExpiryDay, DateTimeOffset now)
    {
        Apply(name, color, isPaid, requiresApproval, deductsFromBalance, daysPerYear, accrualMethod, carriesOver,
            maxCarryOverDays, carryOverExpiryMonth, carryOverExpiryDay);
        Touch(now);
    }

    /// <summary>The day carried-over days expire in <paramref name="year"/>, or null when they never do.</summary>
    public DateOnly? CarryOverExpiresOn(int year) =>
        CarriesOver && CarryOverExpiryMonth is { } month && CarryOverExpiryDay is { } day ? new DateOnly(year, month, day) : null;

    private void Apply(string name, string color, bool isPaid, bool requiresApproval, bool deductsFromBalance,
        decimal daysPerYear, LeaveAccrualMethod accrualMethod, bool carriesOver, decimal? maxCarryOverDays,
        int? carryOverExpiryMonth, int? carryOverExpiryDay)
    {
        var trimmedColor = Guard.Required(color, nameof(color), 7);
        if (trimmedColor.Length != 7 || trimmedColor[0] != '#' || !trimmedColor[1..].All(Uri.IsHexDigit))
            throw new DomainException("Color must be a hex value such as #2563eb.");
        if (!Enum.IsDefined(accrualMethod)) throw new DomainException("Unknown accrual method.");
        if (deductsFromBalance && daysPerYear is <= 0 or > 366) throw new DomainException("Days per year must be between 0 and 366.");
        if (maxCarryOverDays is < 0) throw new DomainException("The carry-over limit cannot be negative.");
        if ((carryOverExpiryMonth is null) != (carryOverExpiryDay is null))
            throw new DomainException("Give both the month and the day when carried-over days expire, or neither.");
        if (carryOverExpiryMonth is { } month && carryOverExpiryDay is { } day &&
            (month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(2025, month)))
            throw new DomainException("The carry-over expiry date is not a valid day of the year.");

        Name = Guard.Required(name, nameof(name), 80);
        Color = trimmedColor.ToLowerInvariant();
        IsPaid = isPaid;
        RequiresApproval = requiresApproval;
        DeductsFromBalance = deductsFromBalance;
        DaysPerYear = deductsFromBalance ? decimal.Round(daysPerYear, 2) : 0;
        AccrualMethod = accrualMethod;
        CarriesOver = deductsFromBalance && carriesOver;
        MaxCarryOverDays = CarriesOver ? maxCarryOverDays : null;
        CarryOverExpiryMonth = CarriesOver ? carryOverExpiryMonth : null;
        CarryOverExpiryDay = CarriesOver ? carryOverExpiryDay : null;
    }
}
