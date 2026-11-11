namespace HrManagement.Domain;

/// <summary>How the yearly allowance is earned.</summary>
public enum LeaveAccrualMethod
{
    /// <summary>Days per year ÷ 12 for every month the person is employed (20 a year ≈ 1.67 a month).</summary>
    Monthly = 0,
    /// <summary>The full yearly amount on 1 January (or on the first day of employment that year).</summary>
    YearlyUpfront = 1
}
